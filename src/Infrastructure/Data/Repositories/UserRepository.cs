using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RhSensoWebApi.Core.Entities;
using RhSensoWebApi.Core.Interfaces;
using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Infrastructure.Data.Context;

namespace RhSensoWebApi.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserRepository> _logger;
    
    public UserRepository(AppDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.CdUsuario == username);
    }
    
    public async Task<User?> GetByUsernameAndPasswordAsync(string username, string passwordHash)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.CdUsuario == username && u.SenhaUser == passwordHash && u.FlAtivo);
    }
    
    public async Task<List<PermissionDto>> GetUserPermissionsAsync(string userId)
    {
        try
        {
            // Query baseada na SQL de referência fornecida
            var permissions = await _context.UserGroups
                .AsNoTracking()
                .Where(u => u.CdUsuario == userId && u.DtFimVal == null)
                .Join(_context.GroupPermissions,
                    u => new { u.CdGrUser, u.CdSistema },
                    h => new { h.CdGrUser, h.CdSistema },
                    (u, h) => new { UserGroup = u, Permission = h })
                .Join(_context.Systems,
                    uh => uh.UserGroup.CdSistema,
                    s => s.CdSistema,
                    (uh, s) => new { uh.UserGroup, uh.Permission, System = s })
                .Where(result => result.System.Ativo)
                .Select(result => new PermissionDto
                {
                    CdSistema = result.Permission.CdSistema,
                    CdFuncao = result.Permission.CdFuncao,
                    CdAcoes = result.Permission.CdAcoes,
                    CdRestric = result.Permission.CdRestric
                })
                .OrderBy(p => p.CdSistema)
                .ThenBy(p => p.CdFuncao)
                .ToListAsync();
                
            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar permissões do usuário: {UserId}", userId);
            return new List<PermissionDto>();
        }
    }
}

