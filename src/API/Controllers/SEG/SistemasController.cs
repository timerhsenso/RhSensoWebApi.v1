using Microsoft.AspNetCore.Mvc;
using RhSensoWebApi.Core.Abstractions.SEG.Sistemas;
using RhSenso.Shared.SEG.Sistemas;

namespace RhSensoWebApi.API.Controllers.SEG
{
    [ApiController]
    [Route("api/v1/sistemas")]
    public class SistemasController : ControllerBase
    {
        private readonly ISistemasService _service;
        private readonly ILogger<SistemasController> _logger;

        public SistemasController(ISistemasService service, ILogger<SistemasController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<SistemaListDto>>> GetAll(CancellationToken ct)
            => Ok(await _service.GetAllAsync(ct));

        [HttpGet("{codigo}")]
        public async Task<ActionResult<SistemaListDto>> GetById(string codigo, CancellationToken ct)
        {
            var item = await _service.GetByIdAsync(codigo, ct);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SistemaCreateDto dto, CancellationToken ct)
        {
            try { await _service.CreateAsync(dto, ct); return CreatedAtAction(nameof(GetById), new { codigo = dto.Codigo }, null); }
            catch (ArgumentException ex) { _logger.LogWarning(ex, "Payload inválido"); return ValidationProblem(title: "Dados inválidos", detail: ex.Message, statusCode: 400); }
            catch (InvalidOperationException ex) { _logger.LogWarning(ex, "Conflito"); return Conflict(new { error = ex.Message }); }
        }

        [HttpPut("{codigo}")]
        public async Task<IActionResult> Update(string codigo, [FromBody] SistemaUpdateDto dto, CancellationToken ct)
        {
            try { await _service.UpdateAsync(codigo, dto, ct); return NoContent(); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (ArgumentException ex) { return ValidationProblem(title: "Dados inválidos", detail: ex.Message, statusCode: 400); }
        }

        [HttpDelete("{codigo}")]
        public async Task<IActionResult> Delete(string codigo, CancellationToken ct)
        {
            try { await _service.DeleteAsync(codigo, ct); return NoContent(); }
            catch (KeyNotFoundException) { return NotFound(); }
        }
    }
}
