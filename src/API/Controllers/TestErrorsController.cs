// src/API/Controllers/TestErrorsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RhSensoWebApi.API.Middleware; // AppValidationException, ForbiddenException

namespace RhSensoWebApi.API.Controllers
{
    [ApiController]
    [Route("api/v1/test-errors")]
    [ApiExplorerSettings(IgnoreApi = true)]  // não polui o Swagger
    [AllowAnonymous]                         // <- GARANTE que nenhuma policy/Authorize bloqueie as rotas de teste
    public class TestErrorsController : ControllerBase
    {
        [HttpGet("validation")]
        public IActionResult Validation()
        {
            var errors = new Dictionary<string, string[]>
            {
                ["email"] = new[] { "Campo obrigatório." },
                ["senha"] = new[] { "Tamanho mínimo: 6." }
            };

            // usa a MESMA classe que o ExceptionHandlingMiddleware trata como 400
            throw new AppValidationException("Falha de validação.", errors);
        }

        [HttpGet("unauthorized")]
        public IActionResult UnauthorizedEndpoint() => throw new UnauthorizedAccessException();

        [HttpGet("forbidden")]
        public IActionResult ForbiddenEndpoint() => throw new ForbiddenException();

        [HttpGet("notfound")]
        public IActionResult NotFoundEndpoint() => throw new KeyNotFoundException();

        [HttpGet("conflict")]
        public IActionResult ConflictEndpoint()
        {
            // determinístico para 409 (sem reflexão/SQL)
            throw new DbUpdateConcurrencyException("Simulated concurrency conflict");
        }

        [HttpGet("internal")]
        public IActionResult InternalEndpoint() => throw new Exception("boom");
    }
}
