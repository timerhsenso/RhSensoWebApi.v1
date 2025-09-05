using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Entities;
using System.Security.Claims;

namespace RhSensoWebApi.Core.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user, List<PermissionDto> permissions);
    bool ValidateToken(string token);
    ClaimsPrincipal GetPrincipalFromToken(string token);
}

