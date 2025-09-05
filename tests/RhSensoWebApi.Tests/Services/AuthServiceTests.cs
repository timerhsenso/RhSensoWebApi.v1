using Microsoft.Extensions.Logging;
using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Entities;
using RhSensoWebApi.Core.Interfaces;
using RhSensoWebApi.Core.Services;

namespace RhSensoWebApi.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _tokenServiceMock.Object,
            _cacheServiceMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsE001Error()
    {
        // Arrange
        var request = new LoginRequest { CdUsuario = "testuser", Senha = "password" };
        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("E001", result.Error?.Code);
        Assert.Equal("Usuário Inválido", result.Error?.Message);
    }

    [Fact]
    public async Task LoginAsync_UserInactive_ReturnsE002Error()
    {
        // Arrange
        var request = new LoginRequest { CdUsuario = "testuser", Senha = "password" };
        var user = new User { CdUsuario = "testuser", FlAtivo = false };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("E002", result.Error?.Code);
        Assert.Equal("Usuário Inválido", result.Error?.Message);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsE003Error()
    {
        // Arrange
        var request = new LoginRequest { CdUsuario = "testuser", Senha = "wrongpassword" };
        var user = new User { CdUsuario = "testuser", FlAtivo = true, SenhaUser = "hashedpassword" };

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("wrongpassword", "hashedpassword"))
            .Returns(false);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("E003", result.Error?.Code);
        Assert.Equal("Credenciais Inválidas", result.Error?.Message);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var request = new LoginRequest { CdUsuario = "testuser", Senha = "password" };
        var user = new User
        {
            CdUsuario = "testuser",
            FlAtivo = true,
            SenhaUser = "hashedpassword",
            DcUsuario = "Test User",
            EmailUsuario = "test@test.com",
            Id = 1
        };
        var permissions = new List<PermissionDto>();
        var token = "jwt-token";

        _userRepositoryMock.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyPassword("password", "hashedpassword"))
            .Returns(true);
        _cacheServiceMock.Setup(x => x.GetAsync<List<PermissionDto>>(It.IsAny<string>()))
            .ReturnsAsync((List<PermissionDto>?)null);
        _userRepositoryMock.Setup(x => x.GetUserPermissionsAsync("testuser"))
            .ReturnsAsync(permissions);
        _tokenServiceMock.Setup(x => x.GenerateToken(user, permissions))
            .Returns(token);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(token, result.Data.Token);
        Assert.Equal("testuser", result.Data.UserInfo.CdUsuario);
    }

    [Fact]
    public async Task CheckHabilitacaoAsync_UserHasPermission_ReturnsTrue()
    {
        // Arrange
        var permissions = new List<PermissionDto>
        {
            new() { CdSistema = "SEG", CdFuncao = "USUARIO", CdAcoes = "ACEI", CdRestric = 'L' }
        };

        _cacheServiceMock.Setup(x => x.GetAsync<List<PermissionDto>>("permissions:testuser"))
            .ReturnsAsync(permissions);

        // Act
        var result = await _authService.CheckHabilitacaoAsync("testuser", "SEG", "USUARIO");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckHabilitacaoAsync_UserDoesNotHavePermission_ReturnsFalse()
    {
        // Arrange
        var permissions = new List<PermissionDto>();

        _cacheServiceMock.Setup(x => x.GetAsync<List<PermissionDto>>("permissions:testuser"))
            .ReturnsAsync(permissions);

        // Act
        var result = await _authService.CheckHabilitacaoAsync("testuser", "SEG", "USUARIO");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckBotaoAsync_UserCanPerformAction_ReturnsTrue()
    {
        // Arrange
        var permissions = new List<PermissionDto>
        {
            new() { CdSistema = "SEG", CdFuncao = "USUARIO", CdAcoes = "ACEI", CdRestric = 'L' }
        };

        _cacheServiceMock.Setup(x => x.GetAsync<List<PermissionDto>>("permissions:testuser"))
            .ReturnsAsync(permissions);

        // Act
        var result = await _authService.CheckBotaoAsync("testuser", "SEG", "USUARIO", "I");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckRestricaoAsync_UserHasRestriction_ReturnsCorrectRestriction()
    {
        // Arrange
        var permissions = new List<PermissionDto>
        {
            new() { CdSistema = "SEG", CdFuncao = "USUARIO", CdAcoes = "ACEI", CdRestric = 'P' }
        };

        _cacheServiceMock.Setup(x => x.GetAsync<List<PermissionDto>>("permissions:testuser"))
            .ReturnsAsync(permissions);

        // Act
        var result = await _authService.CheckRestricaoAsync("testuser", "SEG", "USUARIO");

        // Assert
        Assert.Equal('P', result);
    }
}

