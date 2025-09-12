using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RhSensoWeb.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
// using System.Net.Http.Headers; // se não for usar Accept, pode remover

namespace RhSensoWeb.Controllers
{
    // [Authorize] // opcional: descomente se quiser exigir login na Home
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _http;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory http)
        {
            _logger = logger;
            _http = http;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // -----------------------------
        // Testes rápidos da API
        // -----------------------------

        // Adicione esta action no seu HomeController.cs

        [HttpGet("/TestPermissions")]
        [Authorize] // garante que só usuários logados acessem
        public IActionResult TestPermissions() => View();

        // GET /ping-api  -> chama /health da sua API
        [HttpGet("/ping-api")]
        public async Task<IActionResult> PingApi()
        {
            try
            {
                // ✅ usar o client nomeado "Api" (conforme Program.cs)
                var client = _http.CreateClient("Api");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var resp = await client.GetAsync("health");
                var body = await resp.Content.ReadAsStringAsync();

                _logger.LogInformation("PingApi => {Status} {Reason}", (int)resp.StatusCode, resp.ReasonPhrase);
                return Content($"{(int)resp.StatusCode} {resp.ReasonPhrase} - {body}", "text/plain");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao chamar /health na API.");
                return StatusCode(500, "Falha ao chamar a API (/health). Veja os logs.");
            }
        }

        // GET /ping-ready  -> chama /health/ready da sua API
        [HttpGet("/ping-ready")]
        public async Task<IActionResult> PingReady()
        {
            try
            {
                // ✅ idem: usar "Api"
                var client = _http.CreateClient("Api");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var resp = await client.GetAsync("health/ready");
                var body = await resp.Content.ReadAsStringAsync();

                _logger.LogInformation("PingReady => {Status} {Reason}", (int)resp.StatusCode, resp.ReasonPhrase);
                return Content($"{(int)resp.StatusCode} {resp.ReasonPhrase} - {body}", "text/plain");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao chamar /health/ready na API.");
                return StatusCode(500, "Falha ao chamar a API (/health/ready). Veja os logs.");
            }
        }

        // cole temporariamente no HomeController para testar:
        [HttpGet("/debug/perms1")]
        public async Task<IActionResult> DebugPerms1([FromServices] RhSensoWeb.Services.Security.IPermissionProvider prov)
        {
            var list = await prov.GetAsync();
            return Content($"Permissões carregadas: {list.Count}", "text/plain");
        }

    }
}
