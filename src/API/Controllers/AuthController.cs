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
    /// </summary>
    [Authorize]
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(BaseResponse<List<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions()
    {
        // O TokenService grava ClaimTypes.Name = CdUsuario
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

        return Ok(new BaseResponse<List<PermissionDto>>
        {
            Success = true,
            Data = permissions,
            Message = $"Total: {permissions.Count}"
        });
    }

    [Authorize]
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(new
        {
            name = User.Identity?.Name, // esperado: seu cdusuario
            claims
        });
    }


}
