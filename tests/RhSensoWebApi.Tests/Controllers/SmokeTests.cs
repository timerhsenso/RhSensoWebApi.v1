using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RhSensoWebApi.Tests
{
    /// <summary>
    /// Smoke básico para garantir que o host sobe e responde healthcheck.
    /// </summary>
    public class SmokeTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public SmokeTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Health_Should_Return_OK()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
    }
}
