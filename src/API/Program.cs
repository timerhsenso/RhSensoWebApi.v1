// --------------------------------------------------------------------------------------
//  FASE 1 — Segurança de Produção (CORS / HTTPS / JWT / Segredos)
//  Por quê: reduzir superfície de ataque e aplicar mínimos de produção.
//  Este arquivo está amplamente documentado para que qualquer pessoa entenda as decisões.
//
//  ✅ Itens implementados:
//    - CORS com política nomeada ("Frontends") e origens restritas por ambiente.
//    - HTTPS/HSTS em produção; RequireHttpsMetadata = true fora de DEV.
//    - JWT configurado a partir de segredos.
//    - Fail-fast em produção: sem ConnectionString/Key/AllowedOrigins => não sobe.
//    - Padronização: ConnectionStrings:Default.
//    - Health checks (com readiness mapeado) e raiz amigável.
//    - JSON camelCase + resposta 400 de validação padronizada.
//    - 🔹 Auditoria & Soft-Delete (Item 6): registra interceptor do EF e injeta no DbContext.
// --------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;                    // ApiBehaviorOptions
using Microsoft.AspNetCore.Mvc.Versioning;         // AddApiVersioning / UrlSegmentApiVersionReader
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RhSensoWebApi.API.Middleware;               // Middlewares próprios
using RhSensoWebApi.API.Swagger;                  // Swagger SchemaFilters
using RhSensoWebApi.Core.Abstractions.SEG.Botoes;
using RhSensoWebApi.Core.Abstractions.SEG.Sistemas;
using RhSensoWebApi.Core.Abstractions.SEG.Usuarios;
using RhSensoWebApi.Core.Common.Exceptions;       // BaseResponse, ErrorDto
using RhSensoWebApi.Core.Interfaces;
using RhSensoWebApi.Core.Services;
using RhSensoWebApi.Infrastructure;
using RhSensoWebApi.Infrastructure.Cache;
using RhSensoWebApi.Infrastructure.Data.Context;
using RhSensoWebApi.Infrastructure.Data.Repositories;
using RhSensoWebApi.Infrastructure.Services;
using RhSensoWebApi.Infrastructure.Services.SEG.Botoes;
using RhSensoWebApi.Infrastructure.Services.SEG.Sistemas;
using RhSensoWebApi.Infrastructure.Services.SEG.Usuarios;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;

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

// Validação automática pelo FluentValidation (sem DataAnnotations para evitar duplicidade)
builder.Services.AddFluentValidationAutoValidation(opt =>
{
    opt.DisableDataAnnotationsValidation = true;
});

// Descobre e registra todos os validators deste assembly (inclui BotaoFormValidator)
builder.Services.AddValidatorsFromAssemblyContaining<RhSensoWebApi.API.Validators.SEG.BotaoFormValidator>();

// -------------------------
// API Versioning
// -------------------------
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
    o.ApiVersionReader = new UrlSegmentApiVersionReader();
});
builder.Services.AddVersionedApiExplorer(o =>
{
    o.GroupNameFormat = "'v'VVV";
    o.SubstituteApiVersionInUrl = true;
});

// -------------------------
// ProblemDetails (RFC7807)
// -------------------------
builder.Services.AddProblemDetails();

// -------------------------
// 400 de validação padronizado
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
            TraceId = context.HttpContext.TraceIdentifier,
            Error = new ErrorDto { Code = "VALIDATION_ERROR", Message = "Falha de validação." },
            Timestamp = DateTime.UtcNow
        };

        return new BadRequestObjectResult(resp);
    };
});

// -------------------------
// 🔹 Item 6 — Auditoria & Soft-Delete (DI)
//    - HttpContextAccessor: para identificar usuário no interceptor
//    - Interceptor do EF: aplicado no AddDbContext (abaixo)
// -------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<RhSensoWebApi.Infrastructure.Data.Interceptors.AuditSoftDeleteInterceptor>();

// -------------------------
// Database (EF Core / SQL Server)
// -------------------------
// ⚠️ Alterado para sobrecarga com 'sp' para injetar o interceptor.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));

    // Aplica o interceptor de auditoria/soft delete
    options.AddInterceptors(sp.GetRequiredService<RhSensoWebApi.Infrastructure.Data.Interceptors.AuditSoftDeleteInterceptor>());

    // 1) Log de SQL (vai para console; Serilog captura)
    options.LogTo(Console.WriteLine, LogLevel.Information);

    // 2) (DEV apenas!) Loga parâmetros sensíveis
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

// -------------------------
// Cache (Memória + Redis opcional)
// -------------------------
builder.Services.AddMemoryCache();
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "rhsenso:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// -------------------------
// JWT Authentication
// -------------------------
var jwtSettings = builder.Configuration.GetSection("JWT");
var jwtKey = builder.Configuration["JWT:Key"]; // validado depois no fail-fast

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = string.IsNullOrWhiteSpace(jwtKey)
                ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes("placeholder-key"))
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
// Authorization
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

    c.SchemaFilter<ErrorDtoSchemaExample>();
    c.SchemaFilter<BaseResponseSchemaExample>();
});

// -------------------------
// CORS — Política nomeada "Frontends"
// -------------------------
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var allowedOriginsEnv = builder.Configuration["Cors:AllowedOrigins"];
if (allowedOrigins.Length == 0 && !string.IsNullOrWhiteSpace(allowedOriginsEnv))
{
    allowedOrigins = allowedOriginsEnv.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontends", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            var devFallback = new[]
            {
                "https://localhost:7050", "http://localhost:5189",
                "https://localhost:5173", "http://localhost:5173",
                "https://localhost:5174", "http://localhost:5174"
            };
            var origins = (allowedOrigins.Length > 0) ? allowedOrigins : devFallback;

            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
        else
        {
            if (allowedOrigins.Length == 0)
                throw new InvalidOperationException(
                    "CORS: configure 'Cors:AllowedOrigins' para produção (ex.: https://app.seu-dominio.com;https://admin.seu-dominio.com)");

            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }
    });
});

// -------------------------
// Health Checks
// -------------------------
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddDbContextCheck<AppDbContext>(name: "db");

// -------------------------
// Dependency Injection (Repos/Serviços)
// -------------------------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<RhSensoWebApi.Core.Interfaces.IPasswordHasher, RhSensoWebApi.Infrastructure.Services.PasswordHasher>();
builder.Services.AddScoped<IBotoesService, BotoesService>();
builder.Services.AddScoped<ISistemasService, SistemasService>();
builder.Services.AddScoped<IUsuariosService, UsuariosService>();

// -------------------------
// Fail-fast de produção
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
// BUILD
// ============================================================================
var app = builder.Build();

// ============================================================================
// PIPELINE
// ============================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API v1"));
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// CorrelationId deve vir antes dos logs/exceções para enriquecer tudo que vier depois
app.UseMiddleware<RhSensoWebApi.API.Middleware.CorrelationIdMiddleware>();

app.UseMiddleware<RequestLoggingMiddleware>();     // [1] Logging já com CorrelationId no contexto
app.UseMiddleware<ExceptionHandlingMiddleware>();  // [2] ProblemDetails/BaseResponse com TraceIdentifier alinhado


app.UseCors("Frontends");

app.UseAuthentication();
app.UseAuthorization();

// Health Checks
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = r => r.Name == "self" });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

// Rota raiz amigável
if (app.Environment.IsDevelopment())
    app.MapGet("/", () => Results.Redirect("/swagger"));
else
    app.MapGet("/", () => Results.Text("RhSenso API - OK", "text/plain"));

app.MapControllers();

Serilog.Log.Information("Ambiente: {Env}. CORS habilitado para: {Origins}",
    app.Environment.EnvironmentName,
    string.Join(", ", allowedOrigins.Length > 0 ? allowedOrigins : new[] { "(DEV fallback localhost)" }));

app.Run();

public partial class Program { } // Necessário para testes (WebApplicationFactory<Program>)
