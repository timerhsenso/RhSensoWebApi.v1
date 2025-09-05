// --------------------------------------------------------------------------------------
//  FASE 1 — Segurança de Produção (CORS / HTTPS / JWT / Segredos)
//  Por quê: reduzir superfície de ataque e aplicar mínimos de produção.
//  Este arquivo está amplamente documentado para que qualquer pessoa entenda as decisões.
//
//  ✅ Itens implementados:
//    - CORS com política nomeada ("Frontends") e origens restritas por ambiente.
//      * Em DEV: usa "Cors:AllowedOrigins" ou fallback para localhost.
//      * Em PROD: exige "Cors:AllowedOrigins" (falha se não configurar).
//    - HTTPS/HSTS em produção; RequireHttpsMetadata = true fora de DEV.
//    - JWT configurado a partir de segredos.
//    - Fail-fast em produção: sem ConnectionString/Key/AllowedOrigins => não sobe.
//    - Padronização: ConnectionStrings:Default.
//
//  📦 Como configurar (DEV):
//    dotnet user-secrets init
//    dotnet user-secrets set "ConnectionStrings:Default" "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=true"
//    dotnet user-secrets set "JWT:Key" "<chave-aleatoria-256bits>"
//    // Opcional: cors para DEV
//    dotnet user-secrets set "Cors:AllowedOrigins:0" "https://localhost:5173"
//    dotnet user-secrets set "Cors:AllowedOrigins:1" "http://localhost:5173"
//
//  🏭 Como configurar (PROD) via variáveis de ambiente (exemplos):
//    ConnectionStrings__Default="Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=true"
//    JWT__Key="<chave-256bits-prod>"
//    Cors__AllowedOrigins="https://app.seu-dominio.com;https://admin.seu-dominio.com"
//  (A lista de origens pode vir como array na configuração ou string única separada por ';').
// --------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;                    // InvalidModelStateResponseFactory
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RhSensoWebApi.API.Middleware;               // Middlewares próprios
using RhSensoWebApi.API.Swagger;                  // Swagger SchemaFilters
using RhSensoWebApi.Core.Common.Exceptions;       // BaseResponse, ErrorDto
using RhSensoWebApi.Core.Interfaces;
using RhSensoWebApi.Core.Services;
using RhSensoWebApi.Infrastructure.Cache;
using RhSensoWebApi.Infrastructure.Data.Context;
using RhSensoWebApi.Infrastructure.Data.Repositories;
using RhSensoWebApi.Infrastructure.Services;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// SERILOG — logging estruturado (host logger)
// ============================================================================
builder.Host.UseSerilog((context, config) =>
    config
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/auth-api-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30));

// ============================================================================
// SERVICES (DI) — tudo ANTES do Build()
// ============================================================================

// -------------------------
// Controllers + JSON
// -------------------------
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// -------------------------
// 400 de validação padronizado (errors por campo + traceId)
// -------------------------
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors
                    .Select(err => string.IsNullOrWhiteSpace(err.ErrorMessage) ? "Invalid value." : err.ErrorMessage)
                    .ToArray()
            );

        var resp = new BaseResponse<object>
        {
            Success = false,
            Message = "Falha de validação.",
            Errors = errors,
            TraceId = context.HttpContext.TraceIdentifier
        };

        return new BadRequestObjectResult(resp);
    };
});

// -------------------------
// Database (EF Core / SQL Server)
// - ConnectionStrings:Default deve vir de User Secrets (DEV) ou VARs/KeyVault (PROD)
// -------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));

    // 1) Log de SQL (vai para console; Serilog captura)
    options.LogTo(Console.WriteLine, LogLevel.Information);

    // 2) (DEV apenas!) Loga parâmetros sensíveis
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

// -------------------------
// Cache (Memória + opcional Redis)
// -------------------------
builder.Services.AddMemoryCache();

var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
        options.Configuration = redisConnection);
}

// -------------------------
// JWT Authentication
// - Em PROD, RequireHttpsMetadata = true
// - Chave deve vir de User Secrets/VARs (não versionar)
// -------------------------
var jwtSettings = builder.Configuration.GetSection("JWT");
var jwtKey = builder.Configuration["JWT:Key"]; // não usar null-forgiving; validamos adiante

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;

        // Validação de token
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            // A chave é validada em runtime; falharemos adiante se ela estiver ausente
            IssuerSigningKey = string.IsNullOrWhiteSpace(jwtKey)
                ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes("placeholder-key")) // nunca será usada se falharmos cedo
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// -------------------------
// Authorization (policies/claims podem ser adicionadas depois)
// -------------------------
builder.Services.AddAuthorization();

// -------------------------
// Swagger / OpenAPI (com JWT Bearer)
// -------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Auth API",
        Version = "v1",
        Description = "API de Autenticação e Autorização para Sistemas ERP"
    });

    // JWT Bearer
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. Ex.: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Exemplos — se você adicionou os SchemaFilters no projeto
    c.SchemaFilter<ErrorDtoSchemaExample>();
    c.SchemaFilter<BaseResponseSchemaExample>();
});

// -------------------------
// CORS — Política nomeada "Frontends"
// - Em DEV: usa AllowedOrigins ou fallback para localhost
// - Em PROD: exige AllowedOrigins (falha se não houver)
// -------------------------
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

// Fallback para var de ambiente única separada por ';'
var allowedOriginsEnv = builder.Configuration["Cors:AllowedOrigins"];
if (allowedOrigins.Length == 0 && !string.IsNullOrWhiteSpace(allowedOriginsEnv))
{
    allowedOrigins = allowedOriginsEnv
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontends", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            var devFallback = new[]
            {
                "https://localhost:5173", "http://localhost:5173",
                "https://localhost:5174", "http://localhost:5174"
            };
            var origins = (allowedOrigins.Length > 0) ? allowedOrigins : devFallback;

            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            if (allowedOrigins.Length == 0)
                throw new InvalidOperationException(
                    "CORS: configure 'Cors:AllowedOrigins' para produção (ex.: https://app.seu-dominio.com;https://admin.seu-dominio.com)");

            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// -------------------------
// Health Checks (liveness e readiness)
// -------------------------
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())  // liveness: /health
    .AddDbContextCheck<AppDbContext>();                   // readiness: /health/ready

// -------------------------
// Dependency Injection (Repos/Serviços)
// -------------------------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICacheService, CacheService>();
//builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<RhSensoWebApi.Core.Interfaces.IPasswordHasher, RhSensoWebApi.Infrastructure.Services.PasswordHasher>();


// -------------------------
// Fail-fast de produção (não sobe sem segredos mínimos)
// -------------------------
if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Default")))
        throw new InvalidOperationException("ConnectionStrings:Default não configurada em produção (use variável de ambiente/KeyVault).");

    if (string.IsNullOrWhiteSpace(jwtKey))
        throw new InvalidOperationException("JWT:Key não configurada em produção (use variável de ambiente/KeyVault).");

    if (allowedOrigins.Length == 0)
        throw new InvalidOperationException("Cors:AllowedOrigins deve conter pelo menos 1 origem em produção.");
}

// ============================================================================
// BUILD — a partir daqui temos o 'app'
// ============================================================================
var app = builder.Build();

// ============================================================================
// PIPELINE (middlewares) — tudo DEPOIS do Build()
// ============================================================================

// Swagger — somente em DEV
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API v1"));
}
else
{
    // Em produção, página de erro genérica + HSTS
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// HTTPS redirection
app.UseHttpsRedirection();

// [1] Logging de request/response
app.UseMiddleware<RequestLoggingMiddleware>();

// [2] Tratamento global de exceções => padroniza falhas (BaseResponse)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// CORS — usar SEMPRE a política nomeada
app.UseCors("Frontends");

// AuthN / AuthZ
app.UseAuthentication();
app.UseAuthorization();

// Health Checks
// /health  -> liveness (sem dependências) -> responde 200 em testes
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = r => r.Name == "self"
});

// /health/ready -> readiness (depende de DB) -> pode responder 503 se DB falhar
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true
});

// Controllers
app.MapControllers();

// (Opcional) Migrations automáticas — use conscientemente
/*
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}
*/

// Log de start com resumo de CORS
Serilog.Log.Information("Ambiente: {Env}. CORS habilitado para: {Origins}",
    app.Environment.EnvironmentName,
    string.Join(", ", allowedOrigins.Length > 0 ? allowedOrigins : new[] { "(DEV fallback localhost)" }));

app.Run();

public partial class Program { } // Necessário para testes (WebApplicationFactory<Program>)
