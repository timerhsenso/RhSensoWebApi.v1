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

            return await _db.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.CdUsuario == cdUsuario, ct);
        }

        /// <summary>
        /// Retorna as permissões do usuário conforme regra Delphi:
        /// - usrh1 (vínculo usuário-grupo) com dtfimval IS NULL
        /// - tsistema.ativo = 1 (somente sistemas ativos)
        /// - hbrh1 (habilitações por grupo) juntando por (cdgruser, cdsistema)
        /// </summary>
        public async Task<List<PermissionDto>> GetPermissionsAsync(string cdUsuario, CancellationToken ct = default)
        {
            _logger.LogDebug("Buscando permissões de {CdUsuario}", cdUsuario);

            if (string.IsNullOrWhiteSpace(cdUsuario))
                return new List<PermissionDto>();

            cdUsuario = cdUsuario.Trim();

            // 1) Vínculos válidos do usuário em usrh1 (dtfimval IS NULL)
            var ugQuery =
                from ug in _db.UserGroups.AsNoTracking()
                where ug.CdUsuario == cdUsuario
                      && ug.DtFimVal == null
                select new
                {
                    ug.CdUsuario,
                    ug.CdSistema,
                    ug.CdGrUser
                };

            // 2) Filtrar somente sistemas ativos (tsistema.ativo = 1)
            // Se "Ativo" for int (0/1) no seu mapeamento, troque a linha do where para: where s.Ativo == 1
            var sistemasAtivos =
                from s in _db.Sistemas.AsNoTracking()
                where s.Ativo == true // <-- troque para "s.Ativo == 1" se Ativo for int
                select new
                {
                    s.CdSistema,
                    s.DcSistema
                };

            // 3) Junta usrh1 × tsistema (para manter apenas sistemas ativos)
            var ugAtivos =
                from ug in ugQuery
                join s in sistemasAtivos on ug.CdSistema equals s.CdSistema
                select new
                {
                    ug.CdUsuario,
                    ug.CdSistema,
                    ug.CdGrUser,
                    s.DcSistema
                };

            // 4) Junta com hbrh1 (habilitações por grupo) por (cdgruser, cdsistema)
            var query =
                from ug in ugAtivos
                join gp in _db.GroupPermissions.AsNoTracking()
                    on new { ug.CdGrUser, ug.CdSistema }
                    equals new { gp.CdGrUser, gp.CdSistema }
                orderby ug.CdSistema, ug.CdGrUser, gp.CdFuncao
                select new PermissionDto
                {
                    CdSistema = ug.CdSistema,
                    DcSistema = ug.DcSistema ?? string.Empty,
                    CdGrUser = ug.CdGrUser,
                    CdFuncao = gp.CdFuncao,
                    CdAcoes = gp.CdAcoes ?? string.Empty, // ACEI
                    CdRestric = gp.CdRestric               // L/P/C
                };

            var result = await query.ToListAsync(ct);

            _logger.LogDebug("Permissões encontradas para {CdUsuario}: {Count}", cdUsuario, result.Count);

            return result;
        }
    }
}
