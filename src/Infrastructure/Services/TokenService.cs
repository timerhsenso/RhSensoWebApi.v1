using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Entities.SEG;
using RhSensoWebApi.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RhSensoWebApi.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateToken(Usuario user, List<PermissionDto> permissions)
        {
            try
            {
                var jwt = _configuration.GetSection("JWT");
                var keyBytes = Encoding.ASCII.GetBytes(jwt["Key"]!);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.CdUsuario),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.EmailUsuario ?? string.Empty),

                    new Claim("empresa",       user.CdEmpresa?.ToString() ?? string.Empty),
                    new Claim("filial",        user.CdFilial?.ToString() ?? string.Empty),
                    new Claim("tpusuario",     user.TpUsuario ?? string.Empty),
                    new Claim("flativo",       (user.FlAtivo ?? string.Empty).ToLowerInvariant()),
                    new Claim("idfuncionario", user.IdFuncionario?.ToString() ?? string.Empty),

                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat,
                              DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                              ClaimValueTypes.Integer64)
                };

                if (permissions != null)
                {
                    foreach (var p in permissions)
                    {
                        claims.Add(new Claim("perm", $"{p.CdSistema}:{p.CdFuncao}:{p.CdAcoes}:{p.CdRestric}"));
                    }
                }

                var descriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiryMinutes"]!)),
                    Issuer = jwt["Issuer"],
                    Audience = jwt["Audience"],
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(keyBytes),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var handler = new JwtSecurityTokenHandler();
                var token = handler.CreateToken(descriptor);
                return handler.WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar token para usuário {Usuario}", user.CdUsuario);
                throw;
            }
        }

        public bool ValidateToken(string token)
        {
            try
            {
                var jwt = _configuration.GetSection("JWT");
                var keyBytes = Encoding.ASCII.GetBytes(jwt["Key"]!);

                var handler = new JwtSecurityTokenHandler();
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateIssuer = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwt["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                handler.ValidateToken(token, parameters, out SecurityToken _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var jwt = _configuration.GetSection("JWT");
            var keyBytes = Encoding.ASCII.GetBytes(jwt["Key"]!);

            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateIssuer = true,
                ValidIssuer = jwt["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwt["Audience"],
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero
            };

            return handler.ValidateToken(token, parameters, out SecurityToken _);
        }
    }
}
