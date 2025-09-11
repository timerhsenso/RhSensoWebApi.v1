using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Entities.SEG;
using RhSensoWebApi.Core.Interfaces;
using RhSensoWebApi.Infrastructure.Data.Context;

namespace RhSensoWebApi.Infrastructure.Data.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<UsuarioRepository> _logger;

        public UsuarioRepository(AppDbContext db, ILogger<UsuarioRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<Usuario?> GetByUsernameAsync(string cdUsuario, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(cdUsuario)) return null;
            cdUsuario = cdUsuario.Trim();
            return await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.CdUsuario == cdUsuario, ct);
        }

        public async Task<List<PermissionDto>> GetPermissionsAsync(string cdUsuario, CancellationToken ct = default)
        {
            // TODO: implemente seus joins reais e projete PermissionDto
            _logger.LogDebug("Buscando permissões de {CdUsuario}", cdUsuario);
            return await Task.FromResult(new List<PermissionDto>());
        }
    }
}
