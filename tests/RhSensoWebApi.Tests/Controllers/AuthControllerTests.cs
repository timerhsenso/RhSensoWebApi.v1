using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using RhSensoWebApi.API.Controllers;
using RhSensoWebApi.Core.Interfaces;
using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Common.Exceptions;

namespace RhSensoWebApi.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;
    
    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
    }
    
    [Fact]
    public async Task Login_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new LoginRequest { CdUsuario = "testuser", Senha = "password" };
        var response = new LoginResponse 
        { 
            Success = true, 
            Data = new LoginData 
            { 
                Token = "jwt-token", 
                UserInfo = new UserInfoDto { CdUsuario = "testuser" } 
            } 
        };
        
        _authServiceMock.Setup(x => x.LoginAsync(request))
            .ReturnsAsync(response);
        
        // Act
        var result = await _controller.Login(request);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.True(returnValue.Success);
        Assert.Equal("jwt-token", returnValue.Data?.Token);
    }
    
    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest { CdUsuario = "testuser", Senha = "wrongpassword" };
        var response = new LoginResponse 
        { 
            Success = false, 
            Error = new ErrorDto { Code = "E003", Message = "Credenciais Inválidas" }
        };
        
        _authServiceMock.Setup(x => x.LoginAsync(request))
            .ReturnsAsync(response);
        
        // Act
        var result = await _controller.Login(request);
        
        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var returnValue = Assert.IsType<LoginResponse>(unauthorizedResult.Value);
        Assert.False(returnValue.Success);
        Assert.Equal("E003", returnValue.Error?.Code);
    }
}

