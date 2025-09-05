
using Microsoft.AspNetCore.Mvc;
using RhSenso.Shared.SEG.Botoes;
using RhSensoWeb.Services.ApiClients;
namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    [Route("SEG/Botoes")]
    public class BotoesController : Controller
    {
        private readonly IBotoesApi _api;
        public BotoesController(IBotoesApi api) => _api = api;
        [HttpGet("")]
        public IActionResult Index() => View();
        [HttpGet("list")]
        public async Task<IActionResult> List(string? sistema, string? funcao, int draw = 1, int start = 0, int length = 10,
            string? search = null, string? orderColumn = "CodigoSistema", string? orderDir = "asc")
        {
            int page = (start / Math.Max(1, length)) + 1;
            bool asc = string.Equals(orderDir, "asc", StringComparison.OrdinalIgnoreCase);
            var (total, data) = await _api.ListAsync(sistema, funcao, search, page, length, orderColumn ?? "CodigoSistema", asc);
            return Json(new { draw, recordsTotal = total, recordsFiltered = total, data });
        }
        [HttpGet("create")]
        public IActionResult Create() => View("Form", new BotaoFormDto());
        [HttpGet("edit/{sistema}/{funcao}/{nome}")]
        public async Task<IActionResult> Edit(string sistema, string funcao, string nome)
        {
            var dto = await _api.GetAsync(sistema, funcao, nome);
            if (dto is null) return NotFound();
            return View("Form", dto);
        }
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] BotaoFormDto dto)
        {
            if (!ModelState.IsValid) return View("Form", dto);
            await _api.CreateAsync(dto);
            TempData["Success"] = "Registro criado com sucesso.";
            return RedirectToAction(nameof(Edit), new { sistema = dto.CodigoSistema, funcao = dto.CodigoFuncao, nome = dto.Nome });
        }
        [HttpPost("edit/{sistema}/{funcao}/{nome}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string sistema, string funcao, string nome, [FromForm] BotaoFormDto dto)
        {
            if (!ModelState.IsValid) return View("Form", dto);
            await _api.UpdateAsync(sistema, funcao, nome, dto);
            TempData["Success"] = "Registro atualizado com sucesso.";
            return RedirectToAction(nameof(Edit), new { sistema, funcao, nome });
        }
        [HttpPost("delete/{sistema}/{funcao}/{nome}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string sistema, string funcao, string nome)
        {
            await _api.DeleteAsync(sistema, funcao, nome);
            TempData["Success"] = "Registro excluído com sucesso.";
            return RedirectToAction(nameof(Index));
        }
    }
}
