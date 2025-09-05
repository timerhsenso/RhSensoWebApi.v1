global using Xunit;
global using Microsoft.AspNetCore.Mvc.Testing; // WebApplicationFactory<Program>
global using System.Net;
global using System.Text.Json;
global using Moq; // Mock<>

// 👇 ADICIONE ESTA LINHA (traz a TestApplicationFactory para todos os testes)
global using RhSensoWebApi.Tests.Controllers;
