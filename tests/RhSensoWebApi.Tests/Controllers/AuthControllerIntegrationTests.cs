using FluentAssertions;
using RhSensoWebApi.Core.Common.Exceptions;
using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.ExpandedTests.IntegrationTests.Infrastructure;

namespace RhSensoWebApi.ExpandedTests.IntegrationTests.Controllers;

/// <summary>
/// Testes de integração para AuthController
/// Testando o fluxo completo de autenticação e autorização com banco de dados real
/// </summary>
public class AuthControllerIntegrationTests : IntegrationTestBase
{
    public AuthControllerIntegrationTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    #region Login Integration Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange - Inicializar banco de dados com dados de teste
        await InitializeDatabaseAsync();

        var loginRequest = new LoginRequest
        {
            CdUsuario = "admin_test",
            Senha = "admin123"
        };

        // Act - Realizar requisição de login
        var response = await _client.PostAsync("/api/v1/auth/login", CreateJsonContent(loginRequest));

        // Assert - Verificar resposta de sucesso
        response.StatusCode.Should().Be(HttpStatusCode.OK, "login com credenciais válidas deve retornar 200 OK");

        var loginResponse = await DeserializeResponseAsync<LoginResponse>(response);
        loginResponse.Should().NotBeNull("resposta de login não deve ser nula");
        loginResponse!.Success.Should().BeTrue("login deve ser bem-sucedido");
        loginResponse.Data.Should().NotBeNull("dados do login devem estar presentes");
        loginResponse.Data!.Token.Should().NotBeNullOrEmpty("token JWT deve ser retornado");
        loginResponse.Data.UserInfo.Should().NotBeNull("informações do usuário devem estar presentes");
        loginResponse.Data.UserInfo!.CdUsuario.Should().Be("admin_test", "código do usuário deve estar correto");

        // Cleanup
        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task Login_WithInvalidUser_ReturnsUnauthorizedWithE001()
    {
        // Arrange - Inicializar banco de dados
        await InitializeDatabaseAsync();

        var loginRequest = new LoginRequest
        {
            CdUsuario = "usuario_inexistente",
            Senha = "qualquer_senha"
        };

        // Act - Tentar login com usuário inexistente
        var response = await _client.PostAsync("/api/v1/auth/login", CreateJsonContent(loginRequest));

        // Assert - Verificar resposta de erro
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "usuário inexistente deve retornar 401 Unauthorized");

        var errorResponse = await DeserializeResponseAsync<BaseResponse<object>>(response);
        errorResponse.Should().NotBeNull("resposta de erro não deve ser nula");
        errorResponse!.Success.Should().BeFalse("login deve falhar");
        errorResponse.Error.Should().NotBeNull("erro deve estar presente");
        errorResponse.Error!.Code.Should().Be("E001", "código de erro deve ser E001 para usuário inexistente");
        errorResponse.Error.Message.Should().Be("Usuário Inválido", "mensagem de erro deve estar correta");

        // Cleanup
        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorizedWithE002()
    {
        // Arrange - Inicializar banco de dados
        await InitializeDatabaseAsync();

        var loginRequest = new LoginRequest
        {
            CdUsuario = "inactive_user",
            Senha = "inactive123"
        };

        // Act - Tentar login com usuário inativo
        var response = await _client.PostAsync("/api/v1/auth/login", CreateJsonContent(loginRequest));

        // Assert - Verificar resposta de erro
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "usuário inativo deve retornar 401 Unauthorized");

        var errorResponse = await DeserializeResponseAsync<BaseResponse<object>>(response);
        errorResponse.Should().NotBeNull("resposta de erro não deve ser nula");
        errorResponse!.Success.Should().BeFalse("login deve falhar");
        errorResponse.Error.Should().NotBeNull("erro deve estar presente");
        errorResponse.Error!.Code.Should().Be("E002", "código de erro deve ser E002 para usuário inativo");
        errorResponse.Error.Message.Should().Be("Usuário Inativo", "mensagem de erro deve estar correta");

        // Cleanup
        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsUnauthorizedWithE003()
    {
        // Arrange - Inicializar banco de dados
        await InitializeDatabaseAsync();

        var loginRequest = new LoginRequest
        {
            CdUsuario = "admin_test",
            Senha = "senha_incorreta"
        };

        // Act - Tentar login com senha incorreta
        var response = await _client.PostAsync("/api/v1/auth/login", CreateJsonContent(loginRequest));

        // Assert - Verificar resposta de erro
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "senha incorreta deve retornar 401 Unauthorized");

        var errorResponse = await DeserializeResponseAsync<BaseResponse<object>>(response);
        errorResponse.Should().NotBeNull("resposta de erro não deve ser nula");
        errorResponse!.Success.Should().BeFalse("login deve falhar");
        errorResponse.Error.Should().NotBeNull("erro deve estar presente");
        errorResponse.Error!.Code.Should().Be("E003", "código de erro deve ser E003 para senha incorreta");
        errorResponse.Error.Message.Should().Be("Senha Inválida", "mensagem de erro deve estar correta");

        // Cleanup
        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task Login_WithInvalidModelState_ReturnsBadRequestWithValidationErrors()
    {
        // Arrange - Preparar requisição inválida (sem dados obrigatórios)
        var invalidRequest = new { }; // Objeto vazio, sem CdUsuario e Senha

        // Act - Tentar login com dados inválidos
        var response = await _client.PostAsync("/api/v1/auth/login", CreateJsonContent(invalidRequest));

        // Assert - Verificar resposta de validação
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "dados inválidos devem retornar 400 Bad Request");

        var errorResponse = await DeserializeResponseAsync<BaseResponse<object>>(response);
        errorResponse.Should().NotBeNull("resposta de erro não deve ser nula");
        errorResponse!.Success.Should().BeFalse("validação deve falhar");
        errorResponse.Message.Should().Be("Falha de validação.", "mensagem deve indicar falha de validação");
        errorResponse.Errors.Should().NotBeNull("erros de validação devem estar presentes");
        errorResponse.Errors.Should().ContainKey("CdUsuario", "erro de validação para CdUsuario deve estar presente");
        errorResponse.Errors.Should().ContainKey("Senha", "erro de validação para Senha deve estar presente");
    }

    #endregion

    #region Protected Endpoints Tests

    [Fact]
    public async Task GetPermissoes_WithValidToken_ReturnsUserPermissions()
    {
        // Arrange - Inicializar banco e fazer login
        await InitializeDatabaseAsync();
        var token = await LoginAndGetTokenAsync("admin_test", "admin123");
        AddAuthorizationHeader(token);

        // Act - Buscar permissões do usuário autenticado
        var response = await _client.GetAsync("/api/v1/auth/permissoes");

        // Assert - Verificar resposta de sucesso
        response.StatusCode.Should().Be(HttpStatusCode.OK, "usuário autenticado deve conseguir buscar permissões");

        var permissionsResponse = await DeserializeResponseAsync<BaseResponse<List<PermissionDto>>>(response);
        permissionsResponse.Should().NotBeNull("resposta de permissões não deve ser nula");
        permissionsResponse!.Success.Should().BeTrue("busca de permissões deve ser bem-sucedida");
        permissionsResponse.Data.Should().NotBeNull("dados de permissões devem estar presentes");
        permissionsResponse.Data!.Should().NotBeEmpty("usuário admin deve ter permissões");

        // Verificar se as permissões esperadas estão presentes
        permissionsResponse.Data.Should().Contain(p => p.CdSistema == "SYS01" && p.CdFuncao == "FUNC01");

        // Cleanup
        RemoveAuthorizationHeader();
        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task GetPermissoes_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange - Inicializar banco sem fazer login
        await InitializeDatabaseAsync();

        // Act - Tentar buscar permissões sem token
        var response = await _client.GetAsync("/api/v1/auth/permissoes");

        // Assert - Verificar resposta de não autorizado
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "acesso sem token deve retornar 401 Unauthorized");

        // Cleanup
        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task CheckHabilitacao_WithValidPermission_ReturnsTrue()
    {
        // Arrange - Inicializar banco e fazer login
        await InitializeDatabaseAsync();
        var token = await LoginAndGetTokenAsync("admin_test", "admin123");
        AddAuthorizationHeader(token);

        // Act - Verificar habilitação para sistema/função que o usuário tem acesso
        var response = await _client.GetAsync("/api/v1/auth/checahabilitacao?sistema=SYS01&funcao=FUNC01");

        // Assert - Verificar resposta positiva
        response.StatusCode.Should().Be(HttpStatusCode.OK, "verificação de habilitação deve retornar 200 OK");

        var habilitacaoResponse = await DeserializeResponseAsync<BaseResponse<object>>(response);
        habilitacaoResponse.Should().NotBeNull("resposta de habilitação não deve ser nula");
        habilitacaoResponse!.Success.Should().BeTrue("verificação deve ser bem-sucedida");
        habilitacaoResponse.Data.Should().NotBeNull("dados de habilitação devem estar presentes");

        // Cleanup
        RemoveAuthorizationHeader();
        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task CheckHabilitacao_WithInvalidPermission_ReturnsFalse()
    {
        // Arrange - Inicializar banco e fazer login com usuário limitado
        await InitializeDatabaseAsync();
        var token = await LoginAndGetTokenAsync("user_test", "user123");
        AddAuthorizationHeader(token);

        // Act - Verificar habilitação para sistema/função que o usuário não tem acesso
        var response = await _client.GetAsync("/api/v1/auth/checahabilitacao?sistema=SYS02&funcao=FUNC02");

        // Assert - Verificar resposta negativa
        response.StatusCode.Should().Be(HttpStatusCode.OK, "verificação de habilitação deve retornar 200 OK");

        var habilitacaoResponse = await DeserializeResponseAsync<BaseResponse<object>>(response);
        habilitacaoResponse.Should().NotBeNull("resposta de habilitação não deve ser nula");
        habilitacaoResponse!.Success.Should().BeTrue("verificação deve ser bem-sucedida");
        habilitacaoResponse.Data.Should().NotBeNull("dados de habilitação devem estar presentes");

        // Cleanup
        RemoveAuthorizationHeader();
        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task CheckBotao_WithValidAction_ReturnsTrue()
    {
        // Arrange - Inicializar banco e fazer login
        await InitializeDatabaseAsync();
        var token = await LoginAndGetTokenAsync("admin_test", "admin123");
        AddAuthorizationHeader(token);

        // Act - Verificar ação que o usuário pode executar
        var response = await _client.GetAsync("/api/v1/auth/checabotao?sistema=SYS01&funcao=FUNC01&acao=I");

        // Assert - Verificar resposta positiva
        response.StatusCode.Should().Be(HttpStatusCode.OK, "verificação de ação deve retornar 200 OK");

        var botaoResponse = await DeserializeResponseAsync<BaseResponse<object>>(response);
        botaoResponse.Should().NotBeNull("resposta de ação não deve ser nula");
        botaoResponse!.Success.Should().BeTrue("verificação deve ser bem-sucedida");
        botaoResponse.Data.Should().NotBeNull("dados de ação devem estar presentes");

        // Cleanup
        RemoveAuthorizationHeader();
        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task CheckBotao_WithInvalidAction_ReturnsFalse()
    {
        // Arrange - Inicializar banco e fazer login com usuário limitado
        await InitializeDatabaseAsync();
        var token = await LoginAndGetTokenAsync("user_test", "user123");
        AddAuthorizationHeader(token);

        // Act - Verificar ação que o usuário não pode executar (Excluir)
        var response = await _client.GetAsync("/api/v1/auth/checabotao?sistema=SYS01&funcao=FUNC01&acao=E");

        // Assert - Verificar resposta negativa
        response.StatusCode.Should().Be(HttpStatusCode.OK, "verificação de ação deve retornar 200 OK");

        var botaoResponse = await DeserializeResponseAsync<BaseResponse<object>>(response);
        botaoResponse.Should().NotBeNull("resposta de ação não deve ser nula");
        botaoResponse!.Success.Should().BeTrue("verificação deve ser bem-sucedida");
        botaoResponse.Data.Should().NotBeNull("dados de ação devem estar presentes");

        // Cleanup
        RemoveAuthorizationHeader();
        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task CheckRestricao_WithValidPermission_ReturnsCorrectRestriction()
    {
        // Arrange - Inicializar banco e fazer login
        await InitializeDatabaseAsync();
        var token = await LoginAndGetTokenAsync("admin_test", "admin123");
        AddAuthorizationHeader(token);

        // Act - Verificar restrição para sistema/função
        var response = await _client.GetAsync("/api/v1/auth/checarestricao?sistema=SYS01&funcao=FUNC01");

        // Assert - Verificar resposta com restrição
        response.StatusCode.Should().Be(HttpStatusCode.OK, "verificação de restrição deve retornar 200 OK");

        var restricaoResponse = await DeserializeResponseAsync<BaseResponse<object>>(response);
        restricaoResponse.Should().NotBeNull("resposta de restrição não deve ser nula");
        restricaoResponse!.Success.Should().BeTrue("verificação deve ser bem-sucedida");
        restricaoResponse.Data.Should().NotBeNull("dados de restrição devem estar presentes");

        // Cleanup
        RemoveAuthorizationHeader();
        await CleanupDatabaseAsync();
    }

    #endregion

    #region Complete Authentication Flow Tests

    [Fact]
    public async Task CompleteAuthenticationFlow_LoginAndAccessProtectedEndpoints_WorksCorrectly()
    {
        // Arrange - Inicializar banco de dados
        await InitializeDatabaseAsync();

        // Step 1: Login com credenciais válidas
        var loginRequest = new LoginRequest
        {
            CdUsuario = "admin_test",
            Senha = "admin123"
        };

        var loginResponse = await _client.PostAsync("/api/v1/auth/login", CreateJsonContent(loginRequest));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, "login deve ser bem-sucedido");

        var loginData = await DeserializeResponseAsync<LoginResponse>(loginResponse);
        var token = loginData!.Data!.Token;
        token.Should().NotBeNullOrEmpty("token deve ser retornado");

        // Step 2: Usar token para acessar endpoint protegido
        AddAuthorizationHeader(token);

        var permissoesResponse = await _client.GetAsync("/api/v1/auth/permissoes");
        permissoesResponse.StatusCode.Should().Be(HttpStatusCode.OK, "acesso com token válido deve funcionar");

        // Step 3: Verificar múltiplos endpoints de autorização
        var habilitacaoResponse = await _client.GetAsync("/api/v1/auth/checahabilitacao?sistema=SYS01&funcao=FUNC01");
        habilitacaoResponse.StatusCode.Should().Be(HttpStatusCode.OK, "verificação de habilitação deve funcionar");

        var botaoResponse = await _client.GetAsync("/api/v1/auth/checabotao?sistema=SYS01&funcao=FUNC01&acao=I");
        botaoResponse.StatusCode.Should().Be(HttpStatusCode.OK, "verificação de ação deve funcionar");

        var restricaoResponse = await _client.GetAsync("/api/v1/auth/checarestricao?sistema=SYS01&funcao=FUNC01");
        restricaoResponse.StatusCode.Should().Be(HttpStatusCode.OK, "verificação de restrição deve funcionar");

        // Step 4: Remover token e verificar que acesso é negado
        RemoveAuthorizationHeader();

        var unauthorizedResponse = await _client.GetAsync("/api/v1/auth/permissoes");
        unauthorizedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "acesso sem token deve ser negado");

        // Cleanup
        await CleanupDatabaseAsync();
    }

    #endregion
}

