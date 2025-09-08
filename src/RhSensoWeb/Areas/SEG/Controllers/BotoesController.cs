using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RhSensoWeb.Common.DataTables;

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    public sealed class BotoesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<BotoesController> _logger;

        public BotoesController(IHttpClientFactory httpClientFactory, ILogger<BotoesController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index() => View();

        /// <summary>
        /// Endpoint consumido pelo DataTables.
        /// Lê os parâmetros do DataTables diretamente de Request.Query (evita conflito com propriedades/métodos)
        /// e chama a API /api/v1/botoes enviando asc como "true"/"false" (o ModelBinder da API não aceita "1").
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetData(
            [FromQuery] DataTablesRequest req,
            [FromQuery] string? codigoSistema,
            [FromQuery] string? codigoFuncao,
            CancellationToken ct)
        {
            // Se a API exige filtros, sem eles devolvemos vazio (não quebra a grid)
            if (string.IsNullOrWhiteSpace(codigoSistema) || string.IsNullOrWhiteSpace(codigoFuncao))
            {
                return Json(new
                {
                    draw = req.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = Array.Empty<object>()
                });
            }

            // Paginação
            var pageSize = req.Length <= 0 ? 10 : req.Length;
            var page = (req.Start / pageSize) + 1;

            // Ordenação: ler direto do query do DataTables
            // order[0][dir] = asc|desc
            var dir = (Request.Query["order[0][dir]"].ToString() ?? "").Trim().ToLowerInvariant();
            var asc = dir != "desc"; // default asc

            // order[0][column] e columns[i][data] => tentar descobrir o campo (fallback = "nome")
            string orderBy = "nome";
            var colIndexStr = Request.Query["order[0][column]"].ToString();
            if (int.TryParse(colIndexStr, out var colIndex))
            {
                var key = $"columns[{colIndex}][data]";
                var columnData = Request.Query[key].ToString();
                if (!string.IsNullOrWhiteSpace(columnData))
                    orderBy = columnData;
            }

            // A API quer "true"/"false" (texto) e não "1"/"0"
            var ascText = asc ? "true" : "false";

            // Query da API
            var qb = new StringBuilder();
            qb.Append($"/api/v1/botoes?page={page}");
            qb.Append($"&pageSize={pageSize}");
            qb.Append($"&orderBy={Uri.EscapeDataString(orderBy)}");
            qb.Append($"&asc={ascText}");
            qb.Append($"&codigoSistema={Uri.EscapeDataString(codigoSistema)}");
            qb.Append($"&codigoFuncao={Uri.EscapeDataString(codigoFuncao)}");

            var path = qb.ToString();

            try
            {
                var http = _httpClientFactory.CreateClient("Api"); // configurado no Program.cs
                using var resp = await http.GetAsync(path, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("Falha ao consultar API em {Path}. Status: {Status}. Body: {Body}", path, (int)resp.StatusCode, body);
                    return Json(new
                    {
                        draw = req.Draw,
                        recordsTotal = 0,
                        recordsFiltered = 0,
                        data = Array.Empty<object>()
                    });
                }

                // Aceitar dois formatos:
                // 1) { total: number, data: [...] }
                // 2) { success: true, data: { total: number, data: [...] }, ... }
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                int total = 0;
                JsonElement dataNode;

                // Formato 2 (envelope)
                if (root.TryGetProperty("data", out var envelope)
                    && envelope.ValueKind == JsonValueKind.Object
                    && envelope.TryGetProperty("total", out var totalInEnvelope)
                    && envelope.TryGetProperty("data", out var dataInEnvelope)
                    && dataInEnvelope.ValueKind == JsonValueKind.Array)
                {
                    total = totalInEnvelope.ValueKind == JsonValueKind.Number ? totalInEnvelope.GetInt32() : 0;
                    dataNode = dataInEnvelope;
                }
                // Formato 1 (flat)
                else if (root.TryGetProperty("total", out var totalFlat)
                         && root.TryGetProperty("data", out var dataFlat)
                         && dataFlat.ValueKind == JsonValueKind.Array)
                {
                    total = totalFlat.ValueKind == JsonValueKind.Number ? totalFlat.GetInt32() : 0;
                    dataNode = dataFlat;
                }
                else
                {
                    _logger.LogWarning("Formato inesperado no retorno de {Path}: {Body}", path, body);
                    return Json(new
                    {
                        draw = req.Draw,
                        recordsTotal = 0,
                        recordsFiltered = 0,
                        data = Array.Empty<object>()
                    });
                }

                var rows = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>
                (
                    dataNode.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new List<Dictionary<string, object?>>();

                return Json(new
                {
                    draw = req.Draw,
                    recordsTotal = total,
                    recordsFiltered = total,
                    data = rows
                });
            }
            catch (OperationCanceledException)
            {
                return Json(new
                {
                    draw = req.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = Array.Empty<object>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar {Path}", path);
                return Json(new
                {
                    draw = req.Draw,
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = Array.Empty<object>()
                });
            }
        }
    }
}
