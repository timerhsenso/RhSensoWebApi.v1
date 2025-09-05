using Microsoft.AspNetCore.Mvc;
using RhSensoWebApi.API.Common;

namespace RhSensoWebApi.API.Controllers;

[ApiController]
[Route("api/v1/test-errors")]
public class TestErrorsController : ControllerBase
{
    [HttpGet("validation")]
    public IActionResult Validation()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["login"] = new[] { "Campo obrigatório." }
        };

        return this.FailResponse(
            StatusCodes.Status400BadRequest,
            "Erro de validação",
            "VALIDATION_ERROR",
            errors
        );
    }

    [HttpGet("forbidden")]
    public IActionResult Forbidden()
        => this.FailResponse(StatusCodes.Status403Forbidden, "Acesso negado", "FORBIDDEN");

    [HttpGet("unauthorized")]
    public IActionResult UnauthorizedError()
        => this.FailResponse(StatusCodes.Status401Unauthorized, "Não autorizado", "UNAUTHORIZED");

    [HttpGet("notfound")]
    public IActionResult NotFoundError()
        => this.FailResponse(StatusCodes.Status404NotFound, "Não encontrado", "NOT_FOUND");

    [HttpGet("conflict")]
    public IActionResult ConflictError()
        => this.FailResponse(StatusCodes.Status409Conflict, "Conflito", "CONFLICT");

    [HttpGet("internal")]
    public IActionResult InternalError()
        => this.FailResponse(StatusCodes.Status500InternalServerError, "Erro interno do servidor.", "INTERNAL_ERROR");
}
