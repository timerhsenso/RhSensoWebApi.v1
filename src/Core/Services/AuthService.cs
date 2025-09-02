using Microsoft.Extensions.Logging;
using RhSensoWebApi.Core.Interfaces;
using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Common.Exceptions;

namespace RhSensoWebApi.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ICacheService _cacheService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthService> _logger;
    
    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        ICacheService cacheService,
        IPasswordHasher passwordHasher,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _cacheService = cacheService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }
    
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Iniciando login para usuário: {Usuario}", request.CdUsuario);
            
            // 1. Verificar se usuário existe
            var user = await _userRepository.GetByUsernameAsync(request.CdUsuario);
            if (user == null)
            {
                _logger.LogWarning("Usuário não encontrado: {Usuario}", request.CdUsuario);
                return new LoginResponse
                {
                    Success = false,
                    Error = new ErrorDto { Code = "E001", Message = "Usuário Inválido" }
                };
            }
            
            // 2. Verificar se usuário está ativo
            if (!user.FlAtivo)
            {
                _logger.LogWarning("Usuário inativo: {Usuario}", request.CdUsuario);
                return new LoginResponse
                {
                    Success = false,
                    Error = new ErrorDto { Code = "E002", Message = "Usuário Inválido" }
                };
            }
            
            // 3. Verificar senha
            if (!_passwordHasher.VerifyPassword(request.Senha, user.SenhaUser))
            {
                _logger.LogWarning("Senha incorreta para usuário: {Usuario}", request.CdUsuario);
                return new LoginResponse
                {
                    Success = false,
                    Error = new ErrorDto { Code = "E003", Message = "Credenciais Inválidas" }
                };
            }
            
            // 4. Buscar permissões (com cache)
            var cacheKey = $"permissions:{user.CdUsuario}";
            var permissions = await _cacheService.GetAsync<List<PermissionDto>>(cacheKey);
            
            if (permissions == null)
            {
                permissions = await _userRepository.GetUserPermissionsAsync(user.CdUsuario);
                await _cacheService.SetAsync(cacheKey, permissions, TimeSpan.FromMinutes(30));
            }
            
            // 5. Gerar token JWT
            var token = _tokenService.GenerateToken(user, permissions);
            
            // 6. Preparar resposta
            var userInfo = new UserInfoDto
            {
                CdUsuario = user.CdUsuario,
                DcUsuario = user.DcUsuario,
                NmImpcche = user.NmImpcche,
                TpUsuario = user.TpUsuario,
                NoMatric = user.NoMatric,
                CdEmpresa = user.CdEmpresa,
                CdFilial = user.CdFilial,
                NoUser = user.NoUser,
                EmailUsuario = user.EmailUsuario,
                FlAtivo = user.FlAtivo,
                Id = user.Id,
                NormalizedUsername = user.NormalizedUsername,
                IdFuncionario = user.IdFuncionario,
                FlNaoRecebeEmail = user.FlNaoRecebeEmail
            };
            
            _logger.LogInformation("Login realizado com sucesso para usuário: {Usuario}", request.CdUsuario);
            
            return new LoginResponse
            {
                Success = true,
                Data = new LoginData
                {
                    Token = token,
                    ExpiresIn = 3600, // 1 hora
                    UserInfo = userInfo
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante login do usuário: {Usuario}", request.CdUsuario);
            return new LoginResponse
            {
                Success = false,
                Error = new ErrorDto { Code = "E999", Message = "Erro interno do servidor" }
            };
        }
    }
    
    public async Task<List<PermissionDto>> GetPermissionsAsync(string userId)
    {
        var cacheKey = $"permissions:{userId}";
        var permissions = await _cacheService.GetAsync<List<PermissionDto>>(cacheKey);
        
        if (permissions == null)
        {
            permissions = await _userRepository.GetUserPermissionsAsync(userId);
            await _cacheService.SetAsync(cacheKey, permissions, TimeSpan.FromMinutes(30));
        }
        
        return permissions;
    }
    
    public async Task<bool> CheckHabilitacaoAsync(string userId, string sistema, string funcao)
    {
        var permissions = await GetPermissionsAsync(userId);
        return permissions.Any(p => p.CdSistema == sistema && p.CdFuncao == funcao);
    }
    
    public async Task<bool> CheckBotaoAsync(string userId, string sistema, string funcao, string acao)
    {
        var permissions = await GetPermissionsAsync(userId);
        var permission = permissions.FirstOrDefault(p => p.CdSistema == sistema && p.CdFuncao == funcao);
        
        return permission != null && permission.CdAcoes.Contains(acao);
    }
    
    public async Task<char> CheckRestricaoAsync(string userId, string sistema, string funcao)
    {
        var permissions = await GetPermissionsAsync(userId);
        var permission = permissions.FirstOrDefault(p => p.CdSistema == sistema && p.CdFuncao == funcao);
        
        return permission?.CdRestric ?? 'N'; // N = Nenhuma permissão
    }
}

