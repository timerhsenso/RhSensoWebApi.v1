using Microsoft.Extensions.Logging;
using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Entities;
using RhSensoWebApi.Core.Interfaces;
using RhSensoWebApi.ExpandedTests.Fixtures;
using System.Text;

namespace RhSensoWebApi.ExpandedTests.Helpers;

/// <summary>
/// Helpers para facilitar a criação e configuração de testes
/// Centraliza operações comuns e reduz duplicação de código
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Helper para criação e configuração de mocks
    /// </summary>
    public static class MockHelper
    {
        /// <summary>
        /// Cria um mock configurado do IUserRepository
        /// </summary>
        public static Mock<IUserRepository> CreateUserRepositoryMock()
        {
            var mock = new Mock<IUserRepository>();

            // Configurações padrão que podem ser sobrescritas nos testes
            mock.Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            mock.Setup(x => x.GetUserPermissionsAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<PermissionDto>());

            return mock;
        }

        /// <summary>
        /// Configura o mock do UserRepository para retornar um usuário específico
        /// </summary>
        public static void SetupUserRepositoryWithUser(Mock<IUserRepository> mock, User user, List<PermissionDto>? permissions = null)
        {
            mock.Setup(x => x.GetByUsernameAsync(user.CdUsuario))
                .ReturnsAsync(user);

            if (permissions != null)
            {
                mock.Setup(x => x.GetUserPermissionsAsync(user.CdUsuario))
                    .ReturnsAsync(permissions);
            }
        }

        /// <summary>
        /// Cria um mock configurado do ITokenService
        /// </summary>
        public static Mock<ITokenService> CreateTokenServiceMock()
        {
            var mock = new Mock<ITokenService>();

            // Configuração padrão para gerar token
            mock.Setup(x => x.GenerateToken(It.IsAny<User>(), It.IsAny<List<PermissionDto>>()))
                .Returns("mock_jwt_token_12345");

            // Configuração padrão para validar token
            mock.Setup(x => x.ValidateToken(It.IsAny<string>()))
                .Returns(true);

            return mock;
        }

        /// <summary>
        /// Cria um mock configurado do IPasswordHasher
        /// </summary>
        public static Mock<IPasswordHasher> CreatePasswordHasherMock()
        {
            var mock = new Mock<IPasswordHasher>();

            // Configuração padrão para hash de senha
            mock.Setup(x => x.HashPassword(It.IsAny<string>()))
                .Returns((string password) => $"hashed_{password}");

            // Configuração padrão para verificação de senha (sempre verdadeiro)
            mock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            return mock;
        }

        /// <summary>
        /// Configura o mock do PasswordHasher para cenários específicos
        /// </summary>
        public static void SetupPasswordHasherForUser(Mock<IPasswordHasher> mock, string correctPassword, string hashedPassword)
        {
            mock.Setup(x => x.VerifyPassword(correctPassword, hashedPassword))
                .Returns(true);

            mock.Setup(x => x.VerifyPassword(It.Is<string>(p => p != correctPassword), hashedPassword))
                .Returns(false);
        }

        /// <summary>
        /// Cria um mock configurado do ICacheService
        /// </summary>
        public static Mock<ICacheService> CreateCacheServiceMock()
        {
            var mock = new Mock<ICacheService>();

            // Configuração padrão para cache (sempre retorna null)
            mock.Setup(x => x.GetAsync<object>(It.IsAny<string>())).ReturnsAsync((object?)null);
            mock.Setup(x => x.GetAsync<string>(It.IsAny<string>())).ReturnsAsync((string?)null);

            return mock;
        }

        /// <summary>
        /// Cria um mock configurado do ILogger
        /// </summary>
        public static Mock<ILogger<T>> CreateLoggerMock<T>()
        {
            return new Mock<ILogger<T>>();
        }
    }

    /// <summary>
    /// Helper para operações com JSON
    /// </summary>
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Serializa um objeto para JSON com configurações padrão
        /// </summary>
        public static string Serialize(object obj)
        {
            return JsonSerializer.Serialize(obj, _options);
        }

        /// <summary>
        /// Deserializa JSON para um objeto do tipo especificado
        /// </summary>
        public static T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _options);
        }

        /// <summary>
        /// Cria um StringContent para requisições HTTP
        /// </summary>
        public static StringContent CreateJsonContent(object obj)
        {
            var json = Serialize(obj);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }

    /// <summary>
    /// Helper para verificações comuns em testes
    /// </summary>
    public static class AssertHelper
    {
        /// <summary>
        /// Verifica se uma resposta de login é bem-sucedida
        /// </summary>
        public static void AssertSuccessfulLogin(LoginResponse response, string expectedUsername)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response), "Resposta de login não pode ser nula");

            if (!response.Success)
                throw new AssertionException($"Login deveria ser bem-sucedido, mas falhou: {response.Error?.Message}");

            if (response.Data == null)
                throw new AssertionException("Dados do login não podem ser nulos");

            if (string.IsNullOrEmpty(response.Data.Token))
                throw new AssertionException("Token JWT não pode ser nulo ou vazio");

            if (response.Data.UserInfo == null)
                throw new AssertionException("Informações do usuário não podem ser nulas");

            if (response.Data.UserInfo.CdUsuario != expectedUsername)
                throw new AssertionException($"Username esperado: {expectedUsername}, recebido: {response.Data.UserInfo.CdUsuario}");
        }

        /// <summary>
        /// Verifica se uma resposta de login falhou com o código de erro esperado
        /// </summary>
        public static void AssertFailedLogin(LoginResponse response, string expectedErrorCode)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response), "Resposta de login não pode ser nula");

            if (response.Success)
                throw new AssertionException("Login deveria falhar, mas foi bem-sucedido");

            if (response.Error == null)
                throw new AssertionException("Erro não pode ser nulo quando login falha");

            if (response.Error.Code != expectedErrorCode)
                throw new AssertionException($"Código de erro esperado: {expectedErrorCode}, recebido: {response.Error.Code}");
        }

        /// <summary>
        /// Verifica se uma lista de permissões contém a permissão esperada
        /// </summary>
        public static void AssertPermissionExists(List<PermissionDto> permissions, string sistema, string funcao)
        {
            if (permissions == null)
                throw new ArgumentNullException(nameof(permissions), "Lista de permissões não pode ser nula");

            var permission = permissions.FirstOrDefault(p => p.CdSistema == sistema && p.CdFuncao == funcao);

            if (permission == null)
                throw new AssertionException($"Permissão não encontrada: Sistema={sistema}, Função={funcao}");
        }

        /// <summary>
        /// Verifica se uma lista de permissões NÃO contém a permissão especificada
        /// </summary>
        public static void AssertPermissionNotExists(List<PermissionDto> permissions, string sistema, string funcao)
        {
            if (permissions == null)
                throw new ArgumentNullException(nameof(permissions), "Lista de permissões não pode ser nula");

            var permission = permissions.FirstOrDefault(p => p.CdSistema == sistema && p.CdFuncao == funcao);

            if (permission != null)
                throw new AssertionException($"Permissão não deveria existir: Sistema={sistema}, Função={funcao}");
        }
    }

    /// <summary>
    /// Helper para criação de dados de teste comuns
    /// </summary>
    public static class DataHelper
    {
        /// <summary>
        /// Cria um conjunto padrão de usuários para testes
        /// </summary>
        public static List<User> CreateStandardTestUsers()
        {
            return new List<User>
            {
                TestDataBuilder.User().AsAdmin().WithId(1).Build(),
                TestDataBuilder.User().AsRegularUser().WithId(2).Build(),
                TestDataBuilder.User().AsInactiveUser().WithId(3).Build()
            };
        }

        /// <summary>
        /// Cria um conjunto padrão de permissões para testes
        /// </summary>
        public static List<PermissionDto> CreateStandardTestPermissions()
        {
            return new List<PermissionDto>
            {
                TestDataBuilder.Permission().WithSistema("SYS01").WithFuncao("FUNC01").AsAdminPermission().Build(),
                TestDataBuilder.Permission().WithSistema("SYS01").WithFuncao("FUNC02").AsUserPermission().Build(),
                TestDataBuilder.Permission().WithSistema("SYS02").WithFuncao("FUNC01").AsReadOnlyPermission().Build()
            };
        }

        /// <summary>
        /// Cria permissões específicas para um usuário administrador
        /// </summary>
        public static List<PermissionDto> CreateAdminPermissions()
        {
            return new List<PermissionDto>
            {
                TestDataBuilder.Permission().WithSistema("SYS01").WithFuncao("FUNC01").AsAdminPermission().Build(),
                TestDataBuilder.Permission().WithSistema("SYS01").WithFuncao("FUNC02").AsAdminPermission().Build(),
                TestDataBuilder.Permission().WithSistema("SYS02").WithFuncao("FUNC01").AsAdminPermission().Build(),
                TestDataBuilder.Permission().WithSistema("SYS02").WithFuncao("FUNC02").AsAdminPermission().Build()
            };
        }

        /// <summary>
        /// Cria permissões limitadas para um usuário comum
        /// </summary>
        public static List<PermissionDto> CreateUserPermissions()
        {
            return new List<PermissionDto>
            {
                TestDataBuilder.Permission().WithSistema("SYS01").WithFuncao("FUNC01").AsUserPermission().Build(),
                TestDataBuilder.Permission().WithSistema("SYS01").WithFuncao("FUNC02").AsReadOnlyPermission().Build()
            };
        }
    }

    /// <summary>
    /// Helper para operações de tempo em testes
    /// </summary>
    public static class TimeHelper
    {
        /// <summary>
        /// Executa uma ação e mede o tempo de execução
        /// </summary>
        public static TimeSpan MeasureExecutionTime(Action action)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Executa uma ação assíncrona e mede o tempo de execução
        /// </summary>
        public static async Task<TimeSpan> MeasureExecutionTimeAsync(Func<Task> action)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Verifica se uma operação foi executada dentro do tempo esperado
        /// </summary>
        public static void AssertExecutionTime(Action action, TimeSpan maxExpectedTime, string operationName = "Operação")
        {
            var executionTime = MeasureExecutionTime(action);

            if (executionTime > maxExpectedTime)
            {
                throw new AssertionException(
                    $"{operationName} demorou {executionTime.TotalMilliseconds}ms, " +
                    $"mas deveria ser menor que {maxExpectedTime.TotalMilliseconds}ms");
            }
        }
    }
}

/// <summary>
/// Exceção personalizada para falhas de asserção
/// </summary>
public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
    public AssertionException(string message, Exception innerException) : base(message, innerException) { }
}

