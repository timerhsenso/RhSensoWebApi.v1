using RhSensoWebApi.Core.Entities;
using RhSensoWebApi.Core.DTOs;

namespace RhSensoWebApi.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByUsernameAndPasswordAsync(string username, string passwordHash);
    Task<List<PermissionDto>> GetUserPermissionsAsync(string userId);
}

