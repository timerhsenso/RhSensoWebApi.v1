using System.Collections.Generic;
using System.Threading.Tasks;
using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Interfaces;

namespace RhSensoWebApi.Tests.Common.Fakes;

public class FakeAuthService : IAuthService
{
    public Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CdUsuario) || string.IsNullOrWhiteSpace(request.Senha))
            return Task.FromResult(new LoginResponse { Success = false, Message = "Usuário/senha inválidos" });

        var data = new LoginData
        {
            Token = "fake.jwt.token",
            TokenType = "Bearer",
            ExpiresIn = 1800,
            UserInfo = new UserInfoDto
            {
                CdUsuario = request.CdUsuario,
                DcUsuario = "Teste",
                EmailUsuario = "teste@exemplo.com"
            }
        };

        return Task.FromResult(new LoginResponse { Success = true, Data = data });
    }

    public Task<List<PermissionDto>> GetPermissionsAsync(string userId) => Task.FromResult(new List<PermissionDto>());
    public Task<bool> CheckHabilitacaoAsync(string userId, string sistema, string funcao) => Task.FromResult(true);
    public Task<bool> CheckBotaoAsync(string userId, string sistema, string funcao, string acao) => Task.FromResult(true);
    public Task<char> CheckRestricaoAsync(string userId, string sistema, string funcao) => Task.FromResult('L');
}
