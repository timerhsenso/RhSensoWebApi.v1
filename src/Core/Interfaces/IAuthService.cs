using RhSensoWebApi.Core.DTOs;

namespace RhSensoWebApi.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<List<PermissionDto>> GetPermissionsAsync(string userId);
    Task<bool> CheckHabilitacaoAsync(string userId, string sistema, string funcao);
    Task<bool> CheckBotaoAsync(string userId, string sistema, string funcao, string acao);
    Task<char> CheckRestricaoAsync(string userId, string sistema, string funcao);
}

