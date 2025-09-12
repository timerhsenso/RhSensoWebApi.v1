using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Interfaces;
using RhSensoWebApi.Core.Common.Exceptions;

namespace RhSensoWebApi.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ITokenService _tokenService;
        private readonly ICacheService _cacheService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUsuarioRepository usuarioRepository,
            ITokenService tokenService,
            ICacheService cacheService,
            IPasswordHasher passwordHasher,
            ILogger<AuthService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _tokenService = tokenService;
            _cacheService = cacheService;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        private static bool FlagToBool(string? flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) return false;
            var v = flag.Trim();
            return v.Equals("S", StringComparison.OrdinalIgnoreCase)
                || v.Equals("Y", StringComparison.OrdinalIgnoreCase)
                || v.Equals("1")
                || v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Iniciando login para usuário: {Usuario}", request.CdUsuario);

                var usuario = await _usuarioRepository.GetByUsernameAsync(request.CdUsuario);
                if (usuario == null)
                {
                    _logger.LogWarning("Usuário não encontrado: {Usuario}", request.CdUsuario);
                    return new LoginResponse { Success = false, Error = new ErrorDto { Code = "E001", Message = "Usuário Inválido" } };
                }

                if (!FlagToBool(usuario.FlAtivo))
                {
                    _logger.LogWarning("Usuário inativo: {Usuario}", request.CdUsuario);
                    return new LoginResponse { Success = false, Error = new ErrorDto { Code = "E002", Message = "Usuário Inválido" } };
                }

                // Validação de senha (mantido)
                if (!_passwordHasher.VerifyPassword(request.Senha, usuario.SenhaUser ?? string.Empty))
                {
                    _logger.LogWarning("Senha incorreta para usuário: {Usuario}", request.CdUsuario);
                    return new LoginResponse { Success = false, Error = new ErrorDto { Code = "E003", Message = "Credenciais Inválidas" } };
                }

                // Busca permissões com cache (mantido)
                var cacheKey = $"permissions:{usuario.CdUsuario}";
                var permissions = await _cacheService.GetAsync<List<PermissionDto>>(cacheKey)
                                  ?? await _usuarioRepository.GetPermissionsAsync(usuario.CdUsuario);
                await _cacheService.SetAsync(cacheKey, permissions, TimeSpan.FromMinutes(30));

                // Gera token com permissões (mantido)
                var token = _tokenService.GenerateToken(usuario, permissions);

                // Monta UserInfo (mantido)
                var userInfo = new UserInfoDto
                {
                    CdUsuario = usuario.CdUsuario,
                    DcUsuario = usuario.DcUsuario,
                    NmImpcche = usuario.NmImpCche ?? string.Empty,
                    TpUsuario = usuario.TpUsuario,
                    NoMatric = usuario.NoMatric ?? string.Empty,
                    CdEmpresa = usuario.CdEmpresa?.ToString() ?? string.Empty,
                    CdFilial = usuario.CdFilial?.ToString() ?? string.Empty,
                    NoUser = usuario.NoUser.ToString(),
                    EmailUsuario = usuario.EmailUsuario ?? string.Empty,
                    FlAtivo = FlagToBool(usuario.FlAtivo),
                    Id = 0, // mantido conforme seu DTO atual
                    NormalizedUsername = usuario.NormalizedUserName ?? usuario.CdUsuario.ToUpperInvariant(),
                    IdFuncionario = 0, // mantido conforme seu DTO atual
                    FlNaoRecebeEmail = FlagToBool(usuario.FlNaoRecebeEmail)
                };

                _logger.LogInformation("Login realizado com sucesso para usuário: {Usuario}", request.CdUsuario);

                return new LoginResponse
                {
                    Success = true,
                    Data = new LoginData
                    {
                        Token = token,
                        ExpiresIn = 3600,
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
            if (string.IsNullOrWhiteSpace(userId)) return new();

            var cacheKey = $"permissions:{userId}";
            var permissions = await _cacheService.GetAsync<List<PermissionDto>>(cacheKey);
            if (permissions == null)
            {
                var cdUsuario = userId.Trim();
                permissions = await _usuarioRepository.GetPermissionsAsync(cdUsuario);
                await _cacheService.SetAsync(cacheKey, permissions, TimeSpan.FromMinutes(30));
            }
            return permissions;
        }

        public async Task<bool> CheckHabilitacaoAsync(string userId, string sistema, string funcao)
        {
            var permissions = await GetPermissionsAsync(userId);
            if (permissions.Count == 0) return false;

            return permissions.Any(p =>
                p.CdSistema != null && p.CdFuncao != null &&
                p.CdSistema.Equals(sistema, StringComparison.OrdinalIgnoreCase) &&
                p.CdFuncao.Equals(funcao, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> CheckBotaoAsync(string userId, string sistema, string funcao, string acao)
        {
            var permissions = await GetPermissionsAsync(userId);
            if (permissions.Count == 0) return false;

            var perm = permissions.FirstOrDefault(p =>
                p.CdSistema != null && p.CdFuncao != null &&
                p.CdSistema.Equals(sistema, StringComparison.OrdinalIgnoreCase) &&
                p.CdFuncao.Equals(funcao, StringComparison.OrdinalIgnoreCase));

            if (perm == null || string.IsNullOrEmpty(perm.CdAcoes)) return false;

            var a = (acao ?? string.Empty).Trim();
            if (a.Length == 0) return false;

            // procura a ação (A/C/E/I) sem diferenciar maiúsc/minúsc
            return perm.CdAcoes.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public async Task<char> CheckRestricaoAsync(string userId, string sistema, string funcao)
        {
            var permissions = await GetPermissionsAsync(userId);
            if (permissions.Count == 0) return 'N'; // mantido como padrão atual

            var restricoes = permissions
                .Where(p =>
                    p.CdSistema != null && p.CdFuncao != null &&
                    p.CdSistema.Equals(sistema, StringComparison.OrdinalIgnoreCase) &&
                    p.CdFuncao.Equals(funcao, StringComparison.OrdinalIgnoreCase))
                .Select(p => char.ToUpperInvariant(p.CdRestric))
                .ToList();

            if (restricoes.Count == 0) return 'N';

            // Se vier de múltiplos grupos: escolher a mais restritiva (C > P > L)
            if (restricoes.Contains('C')) return 'C';
            if (restricoes.Contains('P')) return 'P';
            if (restricoes.Contains('L')) return 'L';

            // fallback caso venha algo fora do padrão
            return restricoes.FirstOrDefault('N');
        }
    }
}
