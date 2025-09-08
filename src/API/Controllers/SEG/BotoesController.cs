using Microsoft.AspNetCore.Mvc;
using RhSenso.Shared.Common.DataTables;
using RhSenso.Shared.SEG.Botoes;

namespace RhSensoWebApi.API.Controllers.SEG
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/seg/botoes")]
    [Tags("Botoes")] // agrupa no Swagger
    public sealed class BotoesController : ControllerBase
    {
        // 👉 Quando usar o serviço real, descomente e injete:
        // private readonly IBotoesService _svc;
        // public BotoesController(IBotoesService svc) => _svc = svc;

        // ------------------------------------------------------------------
        // DataTables (server-side)
        // ------------------------------------------------------------------
        [HttpGet("data")]
        public async Task<IActionResult> GetData(
            [FromQuery(Name = "draw")] int draw,
            [FromQuery(Name = "start")] int start = 0,
            [FromQuery(Name = "length")] int length = 10,
            [FromQuery(Name = "search[value]")] string? search = null,
            CancellationToken ct = default)
        {
            // ======= MOCK (substitua pela sua query real) =======
            var all = new List<BotaoListDto> {
                new("SEG","USUARIO","INCLUIR","Incluir usuário","I"),
                new("SEG","USUARIO","EDITAR","Editar usuário","E"),
                new("SEG","USUARIO","EXCLUIR","Excluir usuário","X"),
            }.AsQueryable();
            // =====================================================

            var total = all.Count();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                all = all.Where(x =>
                       x.CodigoSistema.Contains(s, StringComparison.OrdinalIgnoreCase)
                    || x.CodigoFuncao.Contains(s, StringComparison.OrdinalIgnoreCase)
                    || x.Nome.Contains(s, StringComparison.OrdinalIgnoreCase)
                    || x.Descricao.Contains(s, StringComparison.OrdinalIgnoreCase)
                    || x.Acao.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            var filtered = all.Count();

            var page = all
                .OrderBy(x => x.CodigoSistema).ThenBy(x => x.CodigoFuncao).ThenBy(x => x.Nome)
                .Skip(start)
                .Take(Math.Clamp(length, 1, 200))
                .ToList();

            var resp = new DataTableResponse<BotaoListDto>(draw, total, filtered, page);
            return Ok(resp);
        }

        // ------------------------------------------------------------------
        // CRUD tradicional
        // ------------------------------------------------------------------

        /// <summary>Lista (com filtros simples).</summary>
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] string? q = null, CancellationToken ct = default)
        {
            // TODO: var itens = await _svc.ListarAsync(q, ct);
            var itens = new List<BotaoListDto>
            {
                new("SEG","USUARIO","INCLUIR","Incluir usuário","I"),
            };
            if (!string.IsNullOrWhiteSpace(q))
                itens = itens.Where(x =>
                    x.CodigoSistema.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    x.CodigoFuncao.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    x.Nome.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

            return Ok(itens);
        }

        /// <summary>Obtém um item pelo código.</summary>
        [HttpGet("{codigo}")]
        public async Task<IActionResult> ObterPorCodigo(string codigo, CancellationToken ct = default)
        {
            // TODO: var item = await _svc.ObterPorCodigoAsync(codigo, ct);
            var item = new BotaoListDto("SEG", "USUARIO", "INCLUIR", "Incluir usuário", "I");
            if (item is null) return NotFound();
            return Ok(item);
        }

        /// <summary>Cria um novo item.</summary>
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] BotaoFormDto dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            // TODO: var criado = await _svc.CriarAsync(dto, ct);
            var criado = new { codigo = $"{dto.CodigoSistema}:{dto.CodigoFuncao}:{dto.Nome}" };
            return CreatedAtAction(nameof(ObterPorCodigo), new { codigo = criado.codigo, version = "1" }, criado);
        }

        /// <summary>Altera um item existente.</summary>
        [HttpPut("{codigo}")]
        public async Task<IActionResult> Alterar(string codigo, [FromBody] BotaoFormDto dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            // TODO: var ok = await _svc.AlterarAsync(codigo, dto, ct);
            var ok = true;
            return ok ? NoContent() : NotFound();
        }

        /// <summary>Exclui 1 item.</summary>
        [HttpDelete("{codigo}")]
        public async Task<IActionResult> Excluir(string codigo, CancellationToken ct = default)
        {
            // TODO: var ok = await _svc.ExcluirAsync(codigo, ct);
            var ok = true;
            return ok ? NoContent() : NotFound();
        }

        // ------------------------------------------------------------------
        // Operações em lote
        // ------------------------------------------------------------------

        /// <summary>Exclui vários itens de uma vez.</summary>
        [HttpPost("bulk-delete")]
        public async Task<IActionResult> ExcluirEmLote([FromBody] BulkDeleteRequest request, CancellationToken ct = default)
        {
            if (request is null || request.Codigos is null || request.Codigos.Count == 0)
                return BadRequest(new { message = "Envie pelo menos 1 código." });

            // TODO: var removidos = await _svc.ExcluirEmLoteAsync(request.Codigos, ct);
            // TODO: se quiser, retorne também os não encontrados.
            var removidos = request.Codigos.Distinct(StringComparer.OrdinalIgnoreCase).Count();

            return Ok(new
            {
                totalSolicitado = request.Codigos.Count,
                removidos,
                naoEncontrados = Array.Empty<string>() // ajuste se for identificar
            });
        }

        // ------------------------------------------------------------------
        // Utilitário comum de tela (combos/autocomplete)
        // ------------------------------------------------------------------

        /// <summary>Lista simplificada para dropdown/autocomplete.</summary>
        [HttpGet("select")]
        public async Task<IActionResult> Select([FromQuery] string? q = null, CancellationToken ct = default)
        {
            // TODO: var lista = await _svc.SelectAsync(q, ct);
            var lista = new[]
            {
                new { value = "SEG:USUARIO:INCLUIR", label = "Incluir usuário" },
                new { value = "SEG:USUARIO:EDITAR",   label = "Editar usuário" }
            }.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(q))
                lista = lista.Where(x => x.label.Contains(q, StringComparison.OrdinalIgnoreCase));

            return Ok(lista);
        }
    }

    // DTO do bulk delete (pode mover para sua camada Shared depois)
    public sealed class BulkDeleteRequest
    {
        public List<string> Codigos { get; set; } = new();
    }
}
