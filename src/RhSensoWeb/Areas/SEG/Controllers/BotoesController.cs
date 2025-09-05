using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RhSenso.Shared.SEG.Botoes;
using RhSensoWeb.Services.ApiClients;

namespace RhSensoWeb.Areas.SEG.Controllers
{
    /// <summary>
    /// Controller MVC da APP para Botões (consome a API via IBotoesApi).
    /// </summary>
    [Area("SEG")]
    [Route("SEG/Botoes")]
    public class BotoesController : Controller
    {
        private readonly IBotoesApi _api;
        private readonly string _apiBaseUrl;

        /// <param name="api">Cliente HTTP tipado para a API.</param>
        /// <param name="cfg">Usado aqui somente para exibir a URL da API na tela.</param>
        public BotoesController(IBotoesApi api, IConfiguration cfg)
        {
            _api = api;
            _apiBaseUrl = cfg["Api:BaseUrl"] ?? "(Api:BaseUrl não configurado)";
        }

        /// <summary>
        /// Página inicial da listagem (a tabela é carregada por AJAX via ação List).
        /// </summary>
        [HttpGet("")]
        public IActionResult Index()
        {
            // Exibe a URL da API no topo da tela (inclua na View algo como:
            // API: <span class="badge bg-secondary">@ViewBag.ApiBaseUrl</span>)
            ViewBag.ApiBaseUrl = _apiBaseUrl;
            return View();
        }

        /// <summary>
        /// Endpoint usado pelo DataTables (server-side).
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> List(
            string? sistema,
            string? funcao,
            int draw = 1,
            int start = 0,
            int length = 10,
            string? search = null,
            string? orderColumn = "CodigoSistema",
            string? orderDir = "asc")
        {
            // DataTables → página 1-based
            length = Math.Max(1, length);
            start = Math.Max(0, start);
            int page = (start / length) + 1;
            bool asc = string.Equals(orderDir, "asc", StringComparison.OrdinalIgnoreCase);

            // Normaliza o nome da coluna para o contrato esperado pela API
            string orderBy = NormalizeOrderColumn(orderColumn);

            try
            {
                // OBS: a assinatura é (data, total)
                var (data, total) = await _api.ListAsync(
                    sistema, funcao, search, page, length, orderBy, asc);

                return Json(new
                {
                    draw,
                    recordsTotal = total,
                    recordsFiltered = total,
                    data
                });
            }
            catch (HttpRequestException ex)
            {
                // Em caso de erro da API, devolve payload que o DataTables entende
                Response.StatusCode = 500;
                return Json(new
                {
                    draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    error = ex.Message,
                    data = Array.Empty<object>()
                });
            }
        }

        /// <summary>Abre o form vazio.</summary>
        [HttpGet("create")]
        public IActionResult Create()
        {
            ViewBag.ApiBaseUrl = _apiBaseUrl;
            return View("Form", new BotaoFormDto());
        }

        /// <summary>Abre o form preenchido.</summary>
        [HttpGet("edit/{sistema}/{funcao}/{nome}")]
        public async Task<IActionResult> Edit(string sistema, string funcao, string nome)
        {
            var dto = await _api.GetAsync(sistema, funcao, nome);
            if (dto is null) return NotFound();

            ViewBag.ApiBaseUrl = _apiBaseUrl;
            return View("Form", dto);
        }

        /// <summary>Cria um novo registro.</summary>
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] BotaoFormDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ApiBaseUrl = _apiBaseUrl;
                return View("Form", dto);
            }

            try
            {
                await _api.CreateAsync(dto);
                TempData["Success"] = "Registro criado com sucesso.";
                return RedirectToAction(nameof(Edit),
                    new { sistema = dto.CodigoSistema, funcao = dto.CodigoFuncao, nome = dto.Nome });
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.ApiBaseUrl = _apiBaseUrl;
                return View("Form", dto);
            }
        }

        /// <summary>Atualiza um registro existente.</summary>
        [HttpPost("edit/{sistema}/{funcao}/{nome}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string sistema, string funcao, string nome, [FromForm] BotaoFormDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ApiBaseUrl = _apiBaseUrl;
                return View("Form", dto);
            }

            try
            {
                await _api.UpdateAsync(sistema, funcao, nome, dto);
                TempData["Success"] = "Registro atualizado com sucesso.";
                return RedirectToAction(nameof(Edit), new { sistema, funcao, nome });
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.ApiBaseUrl = _apiBaseUrl;
                return View("Form", dto);
            }
        }

        /// <summary>Exclui um registro.</summary>
        [HttpPost("delete/{sistema}/{funcao}/{nome}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string sistema, string funcao, string nome)
        {
            try
            {
                await _api.DeleteAsync(sistema, funcao, nome);
                TempData["Success"] = "Registro excluído com sucesso.";
            }
            catch (HttpRequestException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // ----------------- helpers -----------------

        /// <summary>
        /// Converte nomes de colunas vindas do DataTables para os nomes que a API entende.
        /// </summary>
        private static string NormalizeOrderColumn(string? column)
        {
            if (string.IsNullOrWhiteSpace(column)) return "codigosistema";

            return column.Trim().ToLowerInvariant() switch
            {
                "codigosistema" or "codigo sistema" or "cdsistema" => "codigosistema",
                "codigofuncao" or "codigo funcao" or "cdfuncao" => "codigofuncao",
                "nome" => "nome",
                "descricao" or "descrição" => "descricao",
                "acao" or "ação" => "acao",
                _ => "codigosistema"
            };
        }
    }
}
