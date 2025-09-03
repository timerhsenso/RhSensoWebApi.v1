using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace RhSensoWebApi.Tests.Integration
{
    public class Item0ResponseUniformTests : IClassFixture<TestApplicationFactory>
    {
        private readonly HttpClient _client;

        public Item0ResponseUniformTests(TestApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ModelStateInvalido_DeveRetornar400_ComEnvelopePadrao()
        {
            // Body vazio para disparar [Required]
            var resp = await _client.PostAsJsonAsync("/__test/login", new { });

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();

            json.GetProperty("success").GetBoolean().Should().BeFalse();
            json.GetProperty("message").GetString().Should().Be("Falha de validação.");
            json.TryGetProperty("errors", out var errors).Should().BeTrue("deve existir dicionário de erros");
            json.TryGetProperty("traceId", out var traceId).Should().BeTrue("traceId deve estar presente");
        }

        [Fact]
        public async Task ExcecaoGenerica_DeveRetornar500_ComEnvelopePadrao()
        {
            var resp = await _client.GetAsync("/__test/boom");

            resp.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();

            json.GetProperty("success").GetBoolean().Should().BeFalse();
            json.GetProperty("message").GetString().Should().Be("Erro interno do servidor.");
            json.GetProperty("error").GetProperty("code").GetString().Should().Be("InternalServerError");
            json.TryGetProperty("traceId", out var traceId).Should().BeTrue("traceId deve estar presente");
        }
    }
}
