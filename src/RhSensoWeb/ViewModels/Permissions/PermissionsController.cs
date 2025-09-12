using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RhSensoWeb.Filters;
using RhSensoWeb.ViewModels.Permissions;
using System.Net.Http.Json;

namespace RhSensoWeb.Controllers
{
    [Authorize]
    [NoCache] // <= adiciona isso

    public sealed class PermissionsController : Controller
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(IHttpClientFactory http, ILogger<PermissionsController> logger)
        {
            _http = http;
            _logger = logger;
        }

        private sealed class ApiBaseResponse<T>
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            try
            {
                var client = _http.CreateClient("Api");
                var resp = await client.GetFromJsonAsync<ApiBaseResponse<List<PermissionViewModel>>>(
                    "api/v1/auth/permissions", cancellationToken: ct);

                var list = resp?.Data ?? new List<PermissionViewModel>();

                // ordena por sistema/função
                list = list
                    .OrderBy(p => p.CdSistema)
                    .ThenBy(p => p.CdFuncao)
                    .ThenBy(p => p.PermissionCode)
                    .ToList();

                return View(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao obter permissões do usuário");
                TempData["Error"] = "Não foi possível carregar as suas permissões no momento.";
                return View(new List<PermissionViewModel>());
            }
        }
    }
}
