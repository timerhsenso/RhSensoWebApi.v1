using Microsoft.AspNetCore.Mvc;
using RhSensoWeb.Models;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace RhSensoWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _http;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory http)
        {
            _logger = logger;
            _http = http;
        }

        public IActionResult Index() => View();

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // -----------------------------
        // Testes rápidos da API
        // -----------------------------

        // GET /ping-api  -> chama /health da sua API
        [HttpGet("/ping-api")]
        public async Task<IActionResult> PingApi()
        {
            try
            {
                var client = _http.CreateClient("RhApi"); // baseUrl vem do appsettings
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
                var client = _http.CreateClient("RhApi");
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
    }
}
