using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RhSensoWebApi.Core.Common.Exceptions;
using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Interfaces;
using System.Security.Claims;

namespace RhSensoWebApi.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Autenticação por usuário/senha.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new BaseResponse<object>
            {
                Success = false,
                Error = new ErrorDto { Code = "E400", Message = "Dados de entrada inválidos" }
            });
        }

        var result = await _authService.LoginAsync(request);
        if (!result.Success) return Unauthorized(result);
        return Ok(result);
    }

    /// <summary>
    /// Retorna a lista de permissões do usuário autenticado.
    /// IMPORTANTE: Agora com AMBOS endpoints para compatibilidade
    /// </summary>
    [Authorize]
    [HttpGet("permissions")] // endpoint em inglês
    [HttpGet("permissoes")]  // endpoint em português (compatibilidade)
    [ProducesResponseType(typeof(BaseResponse<List<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions()
    {
        var cdUsuario = User.Identity?.Name
                        ?? User.FindFirstValue(ClaimTypes.Name)
                        ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cdUsuario))
        {
            return Unauthorized(new BaseResponse<object>
            {
                Success = false,
                Error = new ErrorDto { Code = "E401", Message = "Usuário não identificado no token" }
            });
        }

        var permissions = await _authService.GetPermissionsAsync(cdUsuario);

        // Log para debug
        _logger.LogInformation("Retornando {Count} permissões para usuário {Usuario}",
            permissions.Count, cdUsuario);

        foreach (var p in permissions.Take(5)) // mostra só as 5 primeiras
        {
            _logger.LogInformation("Permissão: {Sistema}/{Funcao} - Ações: '{Acoes}' - Restrição: '{Restric}'",
                p.CdSistema, p.CdFuncao, p.CdAcoes, p.CdRestric);
        }

        return Ok(new BaseResponse<List<PermissionDto>>
        {
            Success = true,
            Data = permissions,
            Message = $"Total: {permissions.Count}"
        });
    }

    /// <summary>
    /// Endpoints para verificação individual de permissões (usado pelos TagHelpers se necessário)
    /// </summary>
    [Authorize]
    [HttpGet("checahabilitacao")]
    public async Task<IActionResult> CheckHabilitacao([FromQuery] string? sistema, [FromQuery] string? funcao)
    {
        if (string.IsNullOrWhiteSpace(sistema) || string.IsNullOrWhiteSpace(funcao))
            return BadRequest("Sistema e função são obrigatórios");

        var cdUsuario = User.Identity?.Name ?? string.Empty;
        var allowed = await _authService.CheckHabilitacaoAsync(cdUsuario, sistema, funcao);

        return Ok(new BaseResponse<object>
        {
            Success = true,
            Data = new { allowed, sistema, funcao },
            Message = allowed ? "Permitido" : "Negado"
        });
    }

    [Authorize]
    [HttpGet("checabotao")]
    public async Task<IActionResult> CheckBotao(
        [FromQuery] string? sistema,
        [FromQuery] string? funcao,
        [FromQuery] string? acao)
    {
        if (string.IsNullOrWhiteSpace(sistema) || string.IsNullOrWhiteSpace(funcao) || string.IsNullOrWhiteSpace(acao))
            return BadRequest("Sistema, função e ação são obrigatórios");

        var cdUsuario = User.Identity?.Name ?? string.Empty;
        var allowed = await _authService.CheckBotaoAsync(cdUsuario, sistema, funcao, acao);

        return Ok(new BaseResponse<object>
        {
            Success = true,
            Data = new { allowed, sistema, funcao, acao },
            Message = allowed ? "Permitido" : "Negado"
        });
    }

    [Authorize]
    [HttpGet("checarestricao")]
    public async Task<IActionResult> CheckRestricao([FromQuery] string? sistema, [FromQuery] string? funcao)
    {
        if (string.IsNullOrWhiteSpace(sistema) || string.IsNullOrWhiteSpace(funcao))
            return BadRequest("Sistema e função são obrigatórios");

        var cdUsuario = User.Identity?.Name ?? string.Empty;
        var restricao = await _authService.CheckRestricaoAsync(cdUsuario, sistema, funcao);

        return Ok(new BaseResponse<object>
        {
            Success = true,
            Data = new { restricao, sistema, funcao },
            Message = $"Restrição: {restricao}"
        });
    }

    [Authorize]
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(new
        {
            name = User.Identity?.Name,
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
            claims
        });
    }
}