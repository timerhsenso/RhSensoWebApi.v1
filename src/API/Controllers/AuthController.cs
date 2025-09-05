using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RhSensoWebApi.Core.Common.Exceptions;
using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Interfaces;

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
    /// Realiza login do usuário e retorna JWT token
    /// </summary>
    /// <param name="request">Dados de login</param>
    /// <returns>Token JWT e informações do usuário</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new BaseResponse<object>
            {
                Success = false,
                Error = new ErrorDto
                {
                    Code = "E400",
                    Message = "Dados de entrada inválidos"
                }
            });
        }

        var result = await _authService.LoginAsync(request);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Retorna todas as permissões do usuário autenticado
    /// </summary>
    /// <returns>Lista de permissões</returns>
    [HttpGet("permissoes")]
    [Authorize]
    [ProducesResponseType(typeof(BaseResponse<List<PermissionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPermissoes()
    {
        var userId = User.Identity!.Name!;
        var permissions = await _authService.GetPermissionsAsync(userId);

        return Ok(new BaseResponse<List<PermissionDto>>
        {
            Success = true,
            Data = permissions
        });
    }

    /// <summary>
    /// Verifica se usuário tem acesso a uma função específica
    /// </summary>
    /// <param name="sistema">Código do sistema</param>
    /// <param name="funcao">Código da função</param>
    /// <returns>True se tem acesso</returns>
    [HttpGet("checahabilitacao")]
    [Authorize]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckHabilitacao([FromQuery] string sistema, [FromQuery] string funcao)
    {
        if (string.IsNullOrEmpty(sistema) || string.IsNullOrEmpty(funcao))
        {
            return BadRequest(new BaseResponse<object>
            {
                Success = false,
                Error = new ErrorDto { Code = "E400", Message = "Sistema e função são obrigatórios" }
            });
        }

        var userId = User.Identity!.Name!;
        var hasAccess = await _authService.CheckHabilitacaoAsync(userId, sistema, funcao);

        return Ok(new BaseResponse<object>
        {
            Success = true,
            Data = new
            {
                hasAccess,
                sistema,
                funcao
            }
        });
    }

    /// <summary>
    /// Verifica se usuário pode executar uma ação específica
    /// </summary>
    /// <param name="sistema">Código do sistema</param>
    /// <param name="funcao">Código da função</param>
    /// <param name="acao">Ação (I=Incluir, A=Alterar, E=Excluir, C=Consultar)</param>
    /// <returns>True se pode executar a ação</returns>
    [HttpGet("checabotao")]
    [Authorize]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckBotao(
        [FromQuery] string sistema,
        [FromQuery] string funcao,
        [FromQuery] string acao)
    {
        if (string.IsNullOrEmpty(sistema) || string.IsNullOrEmpty(funcao) || string.IsNullOrEmpty(acao))
        {
            return BadRequest(new BaseResponse<object>
            {
                Success = false,
                Error = new ErrorDto { Code = "E400", Message = "Sistema, função e ação são obrigatórios" }
            });
        }

        var userId = User.Identity!.Name!;
        var canPerformAction = await _authService.CheckBotaoAsync(userId, sistema, funcao, acao);

        var descricaoAcao = acao switch
        {
            "I" => "Incluir",
            "A" => "Alterar",
            "E" => "Excluir",
            "C" => "Consultar",
            _ => "Desconhecida"
        };

        return Ok(new BaseResponse<object>
        {
            Success = true,
            Data = new
            {
                canPerformAction,
                sistema,
                funcao,
                acao,
                descricaoAcao
            }
        });
    }

    /// <summary>
    /// Retorna o tipo de restrição do usuário para uma função
    /// </summary>
    /// <param name="sistema">Código do sistema</param>
    /// <param name="funcao">Código da função</param>
    /// <returns>Tipo de restrição (L=Livre, P=Pessoal, C=Coordenador)</returns>
    [HttpGet("checarestricao")]
    [Authorize]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckRestricao([FromQuery] string sistema, [FromQuery] string funcao)
    {
        if (string.IsNullOrEmpty(sistema) || string.IsNullOrEmpty(funcao))
        {
            return BadRequest(new BaseResponse<object>
            {
                Success = false,
                Error = new ErrorDto { Code = "E400", Message = "Sistema e função são obrigatórios" }
            });
        }

        var userId = User.Identity!.Name!;
        var restricao = await _authService.CheckRestricaoAsync(userId, sistema, funcao);

        var descricao = restricao switch
        {
            'L' => "Livre",
            'P' => "Pessoal",
            'C' => "Coordenador",
            _ => "Sem Permissão"
        };

        return Ok(new BaseResponse<object>
        {
            Success = true,
            Data = new
            {
                restricao = restricao.ToString(),
                descricao
            }
        });
    }
}

