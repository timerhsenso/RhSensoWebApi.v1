using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace RhSensoWeb.Controllers
{
    [Authorize]
    public sealed class DebugController : Controller
    {
        private readonly IHttpClientFactory _http;

        public DebugController(IHttpClientFactory http) => _http = http;

        [HttpGet("/Debug/WhoAmI")]
        public async Task<IActionResult> WhoAmI(CancellationToken ct)
        {
            var client = _http.CreateClient("Api"); // já injeta o Bearer a partir do cookie AuthToken
            var resp = await client.GetAsync("api/v1/auth/whoami", ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            return Content(json, "application/json; charset=utf-8");
        }

        [HttpGet("/Debug/Permissions")]
        public async Task<IActionResult> Permissions(CancellationToken ct)
        {
            var client = _http.CreateClient("Api");
            var resp = await client.GetAsync("api/v1/auth/permissions", ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            return Content(json, "application/json; charset=utf-8");
        }


    }


}
