using System.Security.Claims;
using RhSensoWebApi.Core.Entities;
using RhSensoWebApi.Core.DTOs;

namespace RhSensoWebApi.Core.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user, List<PermissionDto> permissions);
    bool ValidateToken(string token);
    ClaimsPrincipal GetPrincipalFromToken(string token);
}

