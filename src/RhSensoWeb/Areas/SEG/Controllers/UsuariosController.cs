using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using RhSenso.Shared.SEG.Usuarios;

namespace RhSensoWeb.Areas.SEG.Controllers
{
    [Area("SEG")]
    public class UsuariosController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _cfg;
        public UsuariosController(IHttpClientFactory httpClientFactory, IConfiguration cfg) { _httpClientFactory = httpClientFactory; _cfg = cfg; }
        private HttpClient GetApi() { var baseUrl = _cfg["Api:BaseUrl"] ?? "https://localhost:5005/"; return new HttpClient { BaseAddress = new Uri(baseUrl) }; }
        public IActionResult Index() => View();
        public IActionResult Form(string? id) => View(model: id);
        [HttpGet] public async Task<IActionResult> GetData(bool exibirInativos = false, CancellationToken ct = default) { using var api = GetApi(); var data = await api.GetFromJsonAsync<List<UsuarioListDto>>($"api/v1/usuarios?exibirInativos={exibirInativos}", ct) ?? new(); return Json(new { data }); }
        [HttpGet] public async Task<IActionResult> GetOne(string id, CancellationToken ct = default) { using var api = GetApi(); var item = await api.GetFromJsonAsync<UsuarioListDto>($"api/v1/usuarios/{id}", ct); return Json(item); }
        [HttpPost] public async Task<IActionResult> Create([FromBody] UsuarioCreateDto dto, CancellationToken ct = default) { using var api = GetApi(); var res = await api.PostAsJsonAsync("api/v1/usuarios", dto, ct); if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, await res.Content.ReadAsStringAsync(ct)); return Ok(); }
        [HttpPut] public async Task<IActionResult> Update(string id, [FromBody] UsuarioUpdateDto dto, CancellationToken ct = default) { using var api = GetApi(); var res = await api.PutAsJsonAsync($"api/v1/usuarios/{id}", dto, ct); if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, await res.Content.ReadAsStringAsync(ct)); return Ok(); }
        [HttpDelete] public async Task<IActionResult> Delete(string id, CancellationToken ct = default) { using var api = GetApi(); var res = await api.DeleteAsync($"api/v1/usuarios/{id}", ct); if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, await res.Content.ReadAsStringAsync(ct)); return Ok(); }
        [HttpPost] public async Task<IActionResult> ResetPasswords([FromBody] List<string> codigos, CancellationToken ct = default) { using var api = GetApi(); var res = await api.PostAsJsonAsync($"api/v1/usuarios/redefinir-senha", codigos, ct); if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, await res.Content.ReadAsStringAsync(ct)); return Ok(); }
        [HttpPost] public async Task<IActionResult> ResetPassword(string id, CancellationToken ct = default) { using var api = GetApi(); var res = await api.PostAsync($"api/v1/usuarios/{id}/redefinir-senha", null, ct); if (!res.IsSuccessStatusCode) return StatusCode((int)res.StatusCode, await res.Content.ReadAsStringAsync(ct)); return Ok(); }
    }
}
