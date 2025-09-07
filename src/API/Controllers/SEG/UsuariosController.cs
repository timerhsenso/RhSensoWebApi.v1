using Microsoft.AspNetCore.Mvc;
using RhSensoWebApi.Core.Abstractions.SEG.Usuarios;
using RhSenso.Shared.SEG.Usuarios;

namespace RhSensoWebApi.API.Controllers.SEG
{
    [ApiController]
    [Route("api/v1/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuariosService _service;
        private readonly ILogger<UsuariosController> _logger;
        public UsuariosController(IUsuariosService service, ILogger<UsuariosController> logger) { _service = service; _logger = logger; }

        [HttpGet] public async Task<ActionResult<List<UsuarioListDto>>> GetAll([FromQuery] bool exibirInativos = false, CancellationToken ct = default) => Ok(await _service.GetAllAsync(exibirInativos, ct));
        [HttpGet("{codigo}")] public async Task<ActionResult<UsuarioListDto>> GetById(string codigo, CancellationToken ct = default) { var item = await _service.GetByIdAsync(codigo, ct); return item is null ? NotFound() : Ok(item); }
        [HttpPost] public async Task<IActionResult> Create([FromBody] UsuarioCreateDto dto, CancellationToken ct = default) { try { await _service.CreateAsync(dto, ct); return CreatedAtAction(nameof(GetById), new { codigo = dto.Codigo }, null); } catch (ArgumentException ex) { return ValidationProblem(title:"Dados inválidos", detail:ex.Message, statusCode:400); } catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); } }
        [HttpPut("{codigo}")] public async Task<IActionResult> Update(string codigo, [FromBody] UsuarioUpdateDto dto, CancellationToken ct = default) { try { await _service.UpdateAsync(codigo, dto, ct); return NoContent(); } catch (KeyNotFoundException) { return NotFound(); } catch (ArgumentException ex) { return ValidationProblem(title:"Dados inválidos", detail:ex.Message, statusCode:400); } }
        [HttpDelete("{codigo}")] public async Task<IActionResult> Delete(string codigo, CancellationToken ct = default) { try { await _service.DeleteAsync(codigo, ct); return NoContent(); } catch (KeyNotFoundException) { return NotFound(); } }
        [HttpPost("redefinir-senha")] public async Task<IActionResult> RedefinirSenha([FromBody] IEnumerable<string> codigos, CancellationToken ct = default) { try { await _service.RedefinirSenhaPadraoAsync(codigos, ct); return Ok(new { message = "Senhas redefinidas." }); } catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); } }
        [HttpPost("{codigo}/redefinir-senha")] public async Task<IActionResult> RedefinirSenhaUsuario(string codigo, CancellationToken ct = default) { try { await _service.RedefinirSenhaPadraoUsuarioAsync(codigo, ct); return Ok(new { message = "Senha redefinida." }); } catch (KeyNotFoundException) { return NotFound(); } }
    }
}
