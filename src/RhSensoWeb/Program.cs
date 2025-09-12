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

            // **Permissões** (serviço usado pelas TagHelpers)
            builder.Services.AddScoped<IPermissionProvider, PermissionProvider>();

            // API base URL (appsettings Api:BaseUrl)
            var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
                ?? throw new InvalidOperationException("Configure 'Api:BaseUrl' em appsettings.*");
            if (!apiBaseUrl.EndsWith("/")) apiBaseUrl += "/";

            // HttpClient "Api" com AuthTokenHandler e Resilience padrão
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
            // Pipeline
            // ---------------------------------------------------------------
            var app = builder.Build();

            app.Logger.LogInformation("➡ API Base URL: {Url}", apiBaseUrl);

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage(); // útil em Development
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseSession();        // precisa antes dos endpoints
            app.UseAuthorization();

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
    /// Injeta Authorization: Bearer {token} nas chamadas à API.
    /// Agora PRIORIZA a Session. Mantém leitura do cookie apenas como fallback.
    /// </summary>
    internal sealed class AuthTokenHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _http;
        public AuthTokenHandler(IHttpContextAccessor http) => _http = http;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var ctx = _http.HttpContext;

            // 1) Session (server-side)
            var token = ctx?.Session.GetString("AuthToken");

            // 2) (fallback) cookie legado
            if (string.IsNullOrWhiteSpace(token))
                token = ctx?.Request.Cookies["AuthToken"];

            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return base.SendAsync(request, ct);
        }
    }
}
