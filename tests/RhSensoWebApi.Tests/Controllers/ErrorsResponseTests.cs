#if DEBUG
namespace RhSensoWebApi.Tests
{
    public class ErrorsResponseTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ErrorsResponseTests(WebApplicationFactory<Program> factory) => _factory = factory;

        [Theory]
        [InlineData("validation", HttpStatusCode.BadRequest, true)]
        [InlineData("unauthorized", HttpStatusCode.Unauthorized, false)]
        [InlineData("forbidden", HttpStatusCode.Forbidden, false)]
        [InlineData("notfound", HttpStatusCode.NotFound, false)]
        [InlineData("conflict", HttpStatusCode.Conflict, false)]
        [InlineData("internal", HttpStatusCode.InternalServerError, false)]
        public async Task Should_Return_Standard_Error_Format(string route, HttpStatusCode expected, bool expectErrorsDict)
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync($"/api/v1/test-errors/{route}");

            // status
            Assert.Equal(expected, resp.StatusCode);

            // content-type
            Assert.Equal("application/json", resp.Content.Headers.ContentType?.MediaType);

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true });

            var root = doc.RootElement;

            // success=false
            Assert.True(root.TryGetProperty("success", out var successProp));
            Assert.False(successProp.GetBoolean());

            // message presente e não vazio
            Assert.True(root.TryGetProperty("message", out var msgProp));
            Assert.False(string.IsNullOrWhiteSpace(msgProp.GetString()));

            // traceId presente e com tamanho razoável
            Assert.True(root.TryGetProperty("traceId", out var traceProp));
            var traceId = traceProp.GetString();
            Assert.False(string.IsNullOrWhiteSpace(traceId));
            Assert.True(traceId!.Length >= 6);

            // timestamp ISO8601 (UTC tolerância 5 min)
            Assert.True(root.TryGetProperty("timestamp", out var tsProp));
            Assert.True(DateTime.TryParse(tsProp.GetString(), out var parsedTs));
            Assert.True((DateTime.UtcNow - parsedTs.ToUniversalTime()) < TimeSpan.FromMinutes(5));

            // validação: errors deve existir; nos demais, não deve existir
            var hasErrorsProp = root.TryGetProperty("errors", out var errorsProp);
            if (expectErrorsDict)
            {
                Assert.True(hasErrorsProp);
                Assert.Equal(JsonValueKind.Object, errorsProp.ValueKind);
                // checagem mínima do conteúdo
                Assert.True(errorsProp.TryGetProperty("email", out _) || errorsProp.TryGetProperty("senha", out _));
            }
            else
            {
                Assert.False(hasErrorsProp);
            }
        }
    }
}
#endif
