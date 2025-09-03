using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;                    // <- Necessário p/ InvalidModelStateResponseFactory
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

using RhSensoWebApi.Infrastructure.Data.Context;
using RhSensoWebApi.Core.Interfaces;
using RhSensoWebApi.Core.Services;
using RhSensoWebApi.Infrastructure.Services;
using RhSensoWebApi.Infrastructure.Data.Repositories;
using RhSensoWebApi.Infrastructure.Cache;

// Middlewares próprios
using RhSensoWebApi.API.Middleware;

// BaseResponse / ErrorDto (seu tipo atual no Core)
using RhSensoWebApi.Core.Common.Exceptions;        // BaseResponse, ErrorDto

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// SERILOG — logging estruturado
// =========================================================
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

// =========================================================
// SERVICES (DI) — tudo ANTES do Build()
// =========================================================

// Controllers + JSON
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Item 0 — 400 de validação padronizado (com errors por campo + traceId)
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
            Errors = errors,                                   // dicionário por campo
            TraceId = context.HttpContext.TraceIdentifier      // traceId no corpo
        };

        return new BadRequestObjectResult(resp);
    };
});


// Database  usar em produção 
/*
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
*/

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));

    // Loga as queries SQL no nível Information
    //options.LogTo(Console.WriteLine, LogLevel.Information);

    options.LogTo(
    log => Serilog.Log.Information(log),
    LogLevel.Information
);


    // Opcional: loga parâmetros
    options.EnableSensitiveDataLogging();
});


// Cache (Memória + opcional Redis)
builder.Services.AddMemoryCache();

var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
        options.Configuration = redisConnection);
}

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JWT");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Em produção, deixe true; para desenvolvimento mantemos false.
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment() ? true : false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization (policies/claims podem ser adicionadas aqui depois)
builder.Services.AddAuthorization();

// Swagger / OpenAPI
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
});

// CORS
builder.Services.AddCors(options =>
{
    // Em produção, RECOMENDADO restringir a origens conhecidas (WithOrigins)
    options.AddDefaultPolicy(policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

// Dependency Injection
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// =========================================================
// BUILD — a partir daqui temos o 'app'
// =========================================================
var app = builder.Build();

// =========================================================
// PIPELINE (middlewares) — tudo DEPOIS do Build()
// =========================================================

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API v1"));
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(); // HSTS apenas em produção
}

app.UseHttpsRedirection();

// Middlewares próprios (ordem importa):
// 1) Logging (request/response)
app.UseMiddleware<RequestLoggingMiddleware>();

// 2) Tratamento global de exceções (Item 0)
//    IMPORTANTE: deve ficar DEPOIS do Build() e ANTES de auth.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// CORS
app.UseCors();

// AuthN / AuthZ
app.UseAuthentication();
app.UseAuthorization();

// Health Checks
app.MapHealthChecks("/health");

// Controllers
app.MapControllers();

// Migrations (opcional — comente/descomente conforme necessidade)
/*
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
}
*/

app.Run();

public partial class Program { } // Necessário para WebApplicationFactory<Program>
