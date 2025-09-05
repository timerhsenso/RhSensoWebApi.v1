using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RhSensoWeb.Services.ApiClients;

var builder = WebApplication.CreateBuilder(args);

// MVC + Sessão
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(60);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// API base URL (appsettings/user-secrets)
var apiBaseUrl = builder.Configuration["Api:BaseUrl"]
    ?? throw new InvalidOperationException("Configure 'Api:BaseUrl' na APP.");
if (!apiBaseUrl.EndsWith("/")) apiBaseUrl += "/";

// Handler que adiciona Authorization: Bearer {token}
builder.Services.AddTransient<AuthTokenHandler>();

// Cliente tipado para a API — **registra IBotoesApi -> BotoesApi**
builder.Services.AddHttpClient<IBotoesApi, BotoesApi>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<AuthTokenHandler>();

builder.Logging.AddConsole();

var app = builder.Build();

// Verificação em tempo de inicialização (log útil)
using (var scope = app.Services.CreateScope())
{
    var ok = scope.ServiceProvider.GetService<IBotoesApi>() is not null;
    app.Logger.LogInformation("IBotoesApi registrado? {Ok}", ok);
}
app.Logger.LogInformation("➡ API Base URL: {Url}", apiBaseUrl);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
// app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Handler para anexar o Bearer Token
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
