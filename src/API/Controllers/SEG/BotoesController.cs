
using Microsoft.AspNetCore.Mvc;
using RhSenso.Shared.SEG.Botoes;
using RhSensoWebApi.Core.Abstractions.SEG.Botoes;
namespace RhSensoWebApi.API.Controllers.SEG
{
    [ApiController]
    [Route("api/v1/botoes")]
    public class BotoesController : ControllerBase
    {
        private readonly IBotoesService _svc;
        public BotoesController(IBotoesService svc) => _svc = svc;
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? sistema, [FromQuery] string? funcao,
            [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string? orderBy = "CodigoSistema", [FromQuery] bool asc = true)
        {
            var (data, total) = await _svc.ListAsync(sistema, funcao, search, page, pageSize, orderBy, asc);
            return Ok(new { total, data });
        }
        [HttpGet("{sistema}/{funcao}/{nome}")]
        public async Task<IActionResult> Get(string sistema, string funcao, string nome)
            => (await _svc.GetAsync(sistema, funcao, nome)) is { } dto ? Ok(dto) : NotFound();
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BotaoFormDto dto)
        {
            await _svc.CreateAsync(dto);
            return CreatedAtAction(nameof(Get), new { sistema = dto.CodigoSistema, funcao = dto.CodigoFuncao, nome = dto.Nome }, dto);
        }
        [HttpPut("{sistema}/{funcao}/{nome}")]
        public async Task<IActionResult> Update(string sistema, string funcao, string nome, [FromBody] BotaoFormDto dto)
        {
            await _svc.UpdateAsync(sistema, funcao, nome, dto);
            return NoContent();
        }
        [HttpDelete("{sistema}/{funcao}/{nome}")]
        public async Task<IActionResult> Delete(string sistema, string funcao, string nome)
        {
            await _svc.DeleteAsync(sistema, funcao, nome);
            return NoContent();
        }
    }
}
