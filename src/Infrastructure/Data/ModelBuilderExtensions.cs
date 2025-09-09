using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RhSensoWebApi.Core.Abstractions.Persistence; // <-- ajustado

namespace RhSensoWebApi.Infrastructure.Data
{
    public static class ModelBuilderExtensions
    {
        /// <summary>
        /// Aplica filtro global de soft delete:
        /// e => !e.IsDeleted para todas as entidades que implementam ISoftDeleteEntity.
        /// </summary>
        public static void ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clr = entityType.ClrType;
                if (!typeof(ISoftDeleteEntity).IsAssignableFrom(clr))
                    continue;

                var p = Expression.Parameter(clr, "e");
                var prop = Expression.Property(p, nameof(ISoftDeleteEntity.IsDeleted));
                var body = Expression.Equal(prop, Expression.Constant(false));
                var lambda = Expression.Lambda(body, p);

                modelBuilder.Entity(clr).HasQueryFilter(lambda);
            }
        }
    }
}
