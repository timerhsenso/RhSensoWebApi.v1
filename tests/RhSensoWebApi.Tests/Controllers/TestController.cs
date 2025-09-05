using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace RhSensoWebApi.Tests
{
    [ApiController]
    [Route("__test")]
    public class TestController : ControllerBase
    {
        public class LoginDto
        {
            [Required]
            public string? Usuario { get; set; }
            [Required]
            public string? Senha { get; set; }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            // Se chegou aqui, a validação passou (Required); só devolvemos algo simples.
            return Ok(new { ok = true });
        }

        [HttpGet("boom")]
        public IActionResult Boom()
        {
            // Endpoint para provocar erro genérico e validar o middleware (500).
            throw new Exception("boom");
        }
    }
}
