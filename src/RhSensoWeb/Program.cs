// -----------------------------------------------------------------------------
// RhSensoWeb - Program.cs (modelo clássico, SEM top-level statements)
// Compatível com Polly v8 via Microsoft.Extensions.Http.Resilience
// -----------------------------------------------------------------------------

using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http.Resilience; // ✅ pacote novo (Resilience Handler)

namespace RhSensoWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ===============================================================
            // MVC + Razor + JSON (camelCase; ignora nulls)
            // ===============================================================
            builder.Services.AddControllersWithViews();
            builder.Services.ConfigureHttpJsonOptions(o =>
            {
                o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                o.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });

            // ===============================================================
            // Sessão + Cache + HttpContextAccessor
            // ===============================================================
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(o =>
            {
                o.IdleTimeout = TimeSpan.FromMinutes(60);
                o.Cookie.HttpOnly = true;
                o.Cookie.IsEssential = true;
            });

            // ===============================================================
            // Autenticação por Cookie (UI) + Autorização
            // ===============================================================
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = ".RhSensoWeb.Auth";
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                });

            builder.Services.AddAuthorization();

            // ===============================================================
            // Anti-forgery (para POST/DELETE via formulário)
            // ===============================================================
            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.Name = ".RhSensoWeb.Antiforgery";
                options.HeaderName = "RequestVerificationToken";
            });

            // ===============================================================
            // URL base da API (obrigatória) - appsettings: "Api": { "BaseUrl": "https://localhost:7051/" }
            // ===============================================================
            var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
                ?? throw new InvalidOperationException("Configure 'Api:BaseUrl' em appsettings.*");
            if (!apiBaseUrl.EndsWith("/")) apiBaseUrl += "/";

            // ===============================================================
            // HttpClient "Api"
            // - AuthTokenHandler: lê cookie/sessão "AuthToken" e injeta "Authorization: Bearer {token}"
            // - AddStandardResilienceHandler: retry/circuit-breaker/timeout (defaults seguros)
            //   ⚠️ NÃO usar AddPolicyHandler (API antiga do Polly)
            // ===============================================================
            builder.Services.AddTransient<AuthTokenHandler>();

            builder.Services.AddHttpClient("Api", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddStandardResilienceHandler(); // ✅ substitui AddPolicyHandler

            // ===============================================================
            // Logging
            // ===============================================================
            builder.Logging.AddConsole();

            var app = builder.Build();

            app.Logger.LogInformation("➡ API Base URL: {Url}", apiBaseUrl);

            // ===============================================================
            // Pipeline HTTP
            // ===============================================================
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication(); // cookie UI
            app.UseSession();        // estado por sessão
            app.UseAuthorization();

            // ===============================================================
            // Rotas (com Áreas)
            // ===============================================================
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }

    // ==========================================================================
    // TIPOS AUXILIARES (depois do Program)
    // ==========================================================================

    /// <summary>
    /// Lê o token do cookie/sessão e injeta Authorization: Bearer {token} nas chamadas à API.
    /// </summary>
    internal sealed class AuthTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _http;
        public AuthTokenHandler(IHttpContextAccessor http) => _http = http;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var ctx = _http.HttpContext;
            var token = ctx?.Request.Cookies["AuthToken"] ?? ctx?.Session.GetString("AuthToken");
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return base.SendAsync(request, ct);
        }
    }
}
