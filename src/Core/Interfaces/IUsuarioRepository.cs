using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Entities.SEG;

namespace RhSensoWebApi.Core.Interfaces
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetByUsernameAsync(string cdUsuario, CancellationToken ct = default);
        Task<List<PermissionDto>> GetPermissionsAsync(string cdUsuario, CancellationToken ct = default);
    }
}
