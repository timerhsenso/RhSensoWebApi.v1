using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RhSensoWebApi.Core.Entities;
using RhSensoWebApi.Core.Entities.SEG;
using RhSensoWebApi.Infrastructure.Data.Context;
using System.Text;

namespace RhSensoWebApi.ExpandedTests.IntegrationTests.Infrastructure;

/// <summary>
/// Classe base para testes de integração
/// Configura o ambiente de teste com banco de dados em memória e dados de teste
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> _factory;
    protected readonly HttpClient _client;

    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove o contexto de banco de dados real
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Adiciona banco de dados em memória para testes
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                    options.EnableSensitiveDataLogging();
                });

                // Configura logging para testes
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Warning);
                });
            });

            builder.UseEnvironment("Testing");
        });

        _client = _factory.CreateClient();
    }

    /// <summary>
    /// Inicializa o banco de dados com dados de teste
    /// </summary>
    protected async Task InitializeDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Limpa o banco de dados
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Adiciona dados de teste
        await SeedTestDataAsync(context);
    }

    /// <summary>
    /// Popula o banco de dados com dados de teste
    /// </summary>
    private async Task SeedTestDataAsync(AppDbContext context)
    {
        // Criar usuários de teste
        var users = new List<User>
        {
            new()
            {
                CdUsuario = "admin_test",
                DcUsuario = "Administrador de Teste",
                SenhaUser = "$2a$11$rQiU8Z8Z8Z8Z8Z8Z8Z8Z8O", // Hash de "admin123"
                FlAtivo = true,
                EmailUsuario = "admin@test.com",
                CdEmpresa = "EMP01",
                CdFilial = "FIL01",
                TpUsuario = "ADMIN",
                Id = 1,
                IdFuncionario = 100
            },
            new()
            {
                CdUsuario = "user_test",
                DcUsuario = "Usuário de Teste",
                SenhaUser = "$2a$11$rQiU8Z8Z8Z8Z8Z8Z8Z8Z8O", // Hash de "user123"
                FlAtivo = true,
                EmailUsuario = "user@test.com",
                CdEmpresa = "EMP01",
                CdFilial = "FIL01",
                TpUsuario = "USER",
                Id = 2,
                IdFuncionario = 200
            },
            new()
            {
                CdUsuario = "inactive_user",
                DcUsuario = "Usuário Inativo",
                SenhaUser = "$2a$11$rQiU8Z8Z8Z8Z8Z8Z8Z8Z8O", // Hash de "inactive123"
                FlAtivo = false,
                EmailUsuario = "inactive@test.com",
                CdEmpresa = "EMP01",
                CdFilial = "FIL01",
                TpUsuario = "USER",
                Id = 3,
                IdFuncionario = 300
            }
        };

        context.Users.AddRange(users);

        // Criar sistemas de teste
        var systems = new List<Sistema>
        {
            new() { CdSistema = "SYS01", Descricao = "Sistema de Teste 1" },
            new() { CdSistema = "SYS02", Descricao = "Sistema de Teste 2" }
        };

        context.Sistemas.AddRange(systems);

        
        // Criar grupos de usuários
        var userGroups = new List<UserGroup>
        {
            new()
            {
                CdUsuario = "admin_test",
                CdGrUser = "ADMIN_GROUP",
                CdSistema = "SYS01",

                DtFimVal = null
            },
            new()
            {
                CdUsuario = "user_test",
                CdGrUser = "USER_GROUP",
                CdSistema = "SYS01",

                DtFimVal = null
            }
        };

        context.UserGroups.AddRange(userGroups);

        // Criar permissões de grupo
        var groupPermissions = new List<GroupPermission>
        {
            new()
            {
                CdGrUser = "ADMIN_GROUP",
                CdSistema = "SYS01",
                CdFuncao = "FUNC01",
                CdAcoes = "IACE",
                CdRestric = 'L'
            },
            new()
            {
                CdGrUser = "ADMIN_GROUP",
                CdSistema = "SYS02",
                CdFuncao = "FUNC02",
                CdAcoes = "IACE",
                CdRestric = 'L'
            },
            new()
            {
                CdGrUser = "USER_GROUP",
                CdSistema = "SYS01",
                CdFuncao = "FUNC01",
                CdAcoes = "IC",
                CdRestric = 'P'
            }
        };

        context.GroupPermissions.AddRange(groupPermissions);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Cria uma requisição HTTP com conteúdo JSON
    /// </summary>
    protected StringContent CreateJsonContent(object obj)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Deserializa o conteúdo da resposta HTTP
    /// </summary>
    protected async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Adiciona token de autorização ao cliente HTTP
    /// </summary>
    protected void AddAuthorizationHeader(string token)
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Remove token de autorização do cliente HTTP
    /// </summary>
    protected void RemoveAuthorizationHeader()
    {
        _client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Realiza login e retorna o token JWT
    /// </summary>
    protected async Task<string> LoginAndGetTokenAsync(string username, string password)
    {
        var loginRequest = new
        {
            CdUsuario = username,
            Senha = password
        };

        var response = await _client.PostAsync("/api/v1/auth/login", CreateJsonContent(loginRequest));

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed: {response.StatusCode} - {errorContent}");
        }

        var loginResponse = await DeserializeResponseAsync<dynamic>(response);
        return loginResponse?.data?.token?.ToString() ?? throw new InvalidOperationException("Token not found in response");
    }

    /// <summary>
    /// Limpa o banco de dados após cada teste
    /// </summary>
    protected async Task CleanupDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
    }
}

