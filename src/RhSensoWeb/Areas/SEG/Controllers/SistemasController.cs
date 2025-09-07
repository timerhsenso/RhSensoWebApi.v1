using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    public class SistemasController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public SistemasController(IHttpClientFactory httpClientFactory){ _httpClientFactory = httpClientFactory; }
        public IActionResult Index() => View();
        [HttpGet]
        public async Task<IActionResult> GetData(CancellationToken ct)
        {
            var client = new HttpClient{ BaseAddress = new Uri("https://localhost:5005/") }; // ajuste
            var data = await client.GetFromJsonAsync<List<SistemaVm>>("api/v1/sistemas", ct) ?? new();
            return Json(new { data });
        }
    }
    public sealed record SistemaVm(string Codigo, string Descricao);
}