using System;
using System.Linq;                 // <- para .ToList()
using System.Security.Claims;      // <- ClaimsPrincipal/ClaimTypes
using Microsoft.AspNetCore.Http;   // <- IHttpContextAccessor
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RhSensoWebApi.Core.Abstractions.Persistence;

namespace RhSensoWebApi.Infrastructure.Data.Interceptors
{
    /// <summary>
    /// Intercepta SaveChanges/SaveChangesAsync para:
    /// - INSERT: preencher CreatedAt/By
    /// - UPDATE: preencher UpdatedAt/By
    /// - DELETE: converter em SOFT DELETE (IsDeleted=true) + DeletedAt/By
    /// Só atua em entidades que implementam IAuditableEntity/ISoftDeleteEntity (opt-in).
    /// </summary>
    public sealed class AuditSoftDeleteInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _http;

        public AuditSoftDeleteInterceptor(IHttpContextAccessor http)
        {
            _http = http;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            ApplyAudit(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ApplyAudit(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ApplyAudit(DbContext? ctx)
        {
            if (ctx is null) return;

            var now = DateTime.UtcNow;
            var user = ResolveUser();

            foreach (var entry in ctx.ChangeTracker.Entries().ToList())
            {
                if (entry.Entity is IAuditableEntity aud)
                {
                    if (entry.State == EntityState.Added)
                    {
                        aud.CreatedAt = now;
                        aud.CreatedBy = user;
                        aud.UpdatedAt = null;
                        aud.UpdatedBy = null;
                        aud.DeletedAt = null;
                        aud.DeletedBy = null;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        aud.UpdatedAt = now;
                        aud.UpdatedBy = user;
                    }
                }

                if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeleteEntity soft)
                {
                    // Converte DELETE para soft delete
                    soft.IsDeleted = true;

                    if (entry.Entity is IAuditableEntity aud2)
                    {
                        aud2.DeletedAt = now;
                        aud2.DeletedBy = user;
                    }

                    entry.State = EntityState.Modified; // grava como update
                }
            }
        }

        /// <summary>
        /// Resolve a identidade do usuário para trilha de auditoria.
        /// Usa apenas APIs nativas (evita extensão FindFirstValue).
        /// </summary>
        private string ResolveUser()
        {
            var principal = _http.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
                return "system";

            // Preferência: NameIdentifier, depois "sub" (JWT), depois Name
            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("sub")?.Value
                ?? principal.Identity!.Name
                ?? "user";
        }
    }
}
