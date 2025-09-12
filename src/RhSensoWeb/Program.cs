// -----------------------------------------------------------------------------
// RhSensoWeb - Program.cs (modelo clássico, SEM top-level statements)
// Compatível com Polly v8 via Microsoft.Extensions.Http.Resilience
// -----------------------------------------------------------------------------

using System; // <- necessário para TimeSpan
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http.Resilience; // ✅ pacote novo (Resilience Handler)
using RhSensoWeb.Services.Security; // ✅ para PermissionProvider (& afins)

namespace RhSensoWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ---------------------------------------------------------------
            // Services
            // ---------------------------------------------------------------
            builder.Services.AddControllersWithViews();
            builder.Services.ConfigureHttpJsonOptions(o =>
            {
                o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                o.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(o =>
            {
                o.IdleTimeout = TimeSpan.FromMinutes(60);
                o.Cookie.HttpOnly = true;
                o.Cookie.IsEssential = true;
            });

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

            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.Name = ".RhSensoWeb.Antiforgery";
                options.HeaderName = "RequestVerificationToken";
            });

            // ✅ **PERMISSÕES** - Registra o serviço usado pelos TagHelpers
            builder.Services.AddScoped<IPermissionProvider, PermissionProvider>();

            // ✅ **LOGS DETALHADOS** - Só em Development para debug
            if (builder.Environment.IsDevelopment())
            {
                builder.Logging.SetMinimumLevel(LogLevel.Information); // ← ADICIONADO: nível mínimo
                builder.Logging.AddFilter("RhSensoWeb.Services.Security", LogLevel.Information);
                builder.Logging.AddFilter("RhSensoWeb.TagHelpers", LogLevel.Information);
                builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Information); // ← ADICIONADO: para ver chamadas HTTP
            }

            // ✅ **API BASE URL** - Configuração da URL da API
            var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
                ?? throw new InvalidOperationException("Configure 'Api:BaseUrl' em appsettings.*");
            if (!apiBaseUrl.EndsWith("/")) apiBaseUrl += "/";

            // ✅ **HTTP CLIENTS** - Cliente tipado para a API com token e resiliência
            builder.Services.AddTransient<AuthTokenHandler>();

            builder.Services.AddHttpClient("Api", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthTokenHandler>()
            .AddStandardResilienceHandler(); // ✅ substitui AddPolicyHandler (Polly v8)

            builder.Logging.AddConsole();

            // ---------------------------------------------------------------
            // Pipeline (Middleware Order é CRÍTICO!)
            // ---------------------------------------------------------------
            var app = builder.Build();

            // ✅ **LOG INICIAL** - Mostra a URL da API no startup
            app.Logger.LogInformation("🚀 RhSensoWeb iniciado");
            app.Logger.LogInformation("📡 API Base URL: {ApiUrl}", apiBaseUrl);
            app.Logger.LogInformation("🌍 Ambiente: {Environment}", app.Environment.EnvironmentName);

            // ✅ **EXCEPTION HANDLING** - Páginas de erro
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage(); // ✅ útil em Development
            }

            // ✅ **PIPELINE PADRÃO** - Ordem correta dos middlewares
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // ⚠️ **ORDEM CRÍTICA**: Authentication → Session → Authorization
            app.UseAuthentication();  // 1️⃣ Primeiro: lê o cookie de autenticação
            app.UseSession();         // 2️⃣ Depois: habilita a sessão (precisa do token para cache)
            app.UseAuthorization();   // 3️⃣ Por último: valida permissões

            // ✅ **ROTAS** - Areas primeiro, depois default
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // ✅ **STARTUP FINALIZADO**
            app.Logger.LogInformation("✅ Aplicação configurada. Iniciando servidor...");
            app.Run();
        }
    }

    // ==========================================================================
    // 🔒 AUTH TOKEN HANDLER - Injeta Bearer Token nas requisições para a API
    // ==========================================================================

    /// <summary>
    /// Injeta Authorization: Bearer {token} nas chamadas à API.
    /// Prioriza a Session (server-side), usa cookie como fallback.
    /// 
    /// FLUXO:
    /// 1. Usuario faz login → token vai para Session["AuthToken"]
    /// 2. TagHelpers fazem requisição → AuthTokenHandler pega token da sessão
    /// 3. Requisição para API vai com "Authorization: Bearer {token}"
    /// </summary>
    internal sealed class AuthTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _http;

        public AuthTokenHandler(IHttpContextAccessor http) => _http = http;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var ctx = _http.HttpContext;

            // 🥇 Prioridade 1: Session (server-side) - mais seguro
            var token = ctx?.Session.GetString("AuthToken");

            // 🥈 Prioridade 2: Cookie (client-side) - fallback para compatibilidade
            if (string.IsNullOrWhiteSpace(token))
                token = ctx?.Request.Cookies["AuthToken"];

            // ✅ Se encontrou token, adiciona no header Authorization
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return base.SendAsync(request, ct);
        }
    }
}