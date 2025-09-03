using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace RhSensoWebApi.Tests.Controllers
{
    public class AuthIntegrationTests : IClassFixture<TestApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuthIntegrationTests(TestApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Login_ValidCredentials_DeveRetornar200_ComEnvelopePadrao()
        {
            // De acordo com o diagnóstico, o DTO pede CdUsuario e Senha.
            // Com JsonNamingPolicy.CamelCase, envie em camelCase: cdUsuario e senha.
            var loginRequest = new
            {
                cdUsuario = "verusa",
                senha = "ABC"
            };

            var resp = await _client.PostAsJsonAsync("/api/v1/Auth/login", loginRequest);

            // Se falhar por algum motivo, vamos capturar o body para debug
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                var lastBody = await resp.Content.ReadAsStringAsync();
                throw new Xunit.Sdk.XunitException($"Esperado 200, mas veio {(int)resp.StatusCode}. Corpo:\n{lastBody}");
            }

            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();

            body.GetProperty("success").GetBoolean().Should().BeTrue();
            body.TryGetProperty("data", out var data).Should().BeTrue("resposta deve conter data");

            // Se a API retorna token:
            if (data.TryGetProperty("token", out var tokenProp))
            {
                tokenProp.GetString().Should().NotBeNullOrEmpty();
            }
        }
    }
}
