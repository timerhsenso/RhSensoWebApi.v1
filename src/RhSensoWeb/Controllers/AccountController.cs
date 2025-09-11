// src/RhSensoWeb/Controllers/AccountController.cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RhSensoWeb.ViewModels.Account;

// DTOs internos (evita acoplar o site ao assembly da API)
file sealed class ApiBaseResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

file sealed class LoginDataDto
{
    public string? Token { get; set; }
    public int ExpiresIn { get; set; }
    public JsonElement? UserInfo { get; set; }
}

// Payload que a API espera em /api/v1/auth/login
file sealed class LoginRequestDto
{
    public string CdUsuario { get; set; } = default!;
    public string Senha { get; set; } = default!;
}

namespace RhSensoWeb.Controllers
{
    public sealed class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IHttpClientFactory httpClientFactory, ILogger<AccountController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var client = _httpClientFactory.CreateClient("Api");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Envia EXATAMENTE o que a API espera
                var payload = new LoginRequestDto
                {
                    CdUsuario = model.UserName!.Trim(),
                    Senha = model.Password!
                };

                var resp = await client.PostAsJsonAsync("api/v1/auth/login", payload, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync(ct);
                    string msg = "Usuário ou senha inválidos.";
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<ApiBaseResponse<object>>(
                            body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (!string.IsNullOrWhiteSpace(parsed?.Message)) msg = parsed!.Message!;
                    }
                    catch { /* ignora parse falho */ }

                    _logger.LogWarning("Falha de autenticação: {Status} - {Body}", (int)resp.StatusCode, body);
                    model.Error = msg;
                    return View(model);
                }

                var ok = await resp.Content.ReadFromJsonAsync<ApiBaseResponse<LoginDataDto>>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

                var token = ok?.Data?.Token;
                var expires = ok?.Data?.ExpiresIn > 0 ? ok!.Data!.ExpiresIn : 3600;

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("Resposta sem token da API.");
                    model.Error = "Não foi possível obter o token.";
                    return View(model);
                }

                // Cookie do token para o AuthTokenHandler
                var cookieExp = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddSeconds(expires);

                Response.Cookies.Append("AuthToken", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = cookieExp
                });

                // (Opcional) Cookie de autenticação local com nome
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new System.Security.Claims.ClaimsPrincipal(
                        new System.Security.Claims.ClaimsIdentity(
                            new[] { new System.Security.Claims.Claim("name", model.UserName!) },
                            CookieAuthenticationDefaults.AuthenticationScheme
                        )
                    )
                );

                // Redireciona para ReturnUrl (se local), senão Home
                if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao chamar API de autenticação");
                model.Error = "Erro ao comunicar com a API. Tente novamente.";
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            Response.Cookies.Delete("AuthToken");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}
