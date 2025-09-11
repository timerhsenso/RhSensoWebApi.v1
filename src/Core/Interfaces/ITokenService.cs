using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Entities.SEG;
using System.Security.Claims;

namespace RhSensoWebApi.Core.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(Usuario user, List<PermissionDto> permissions);
        bool ValidateToken(string token);
        ClaimsPrincipal GetPrincipalFromToken(string token);
    }
}
