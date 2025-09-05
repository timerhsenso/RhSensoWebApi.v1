using System.Net.Http.Json;
using Xunit.Abstractions;

namespace RhSensoWebApi.Tests.Controllers
{
    public class AuthValidationDiagnostics : IClassFixture<TestApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public AuthValidationDiagnostics(TestApplicationFactory factory, ITestOutputHelper output)
        {
            _client = factory.CreateClient();
            _output = output;
        }

        [Fact]
        public async Task Dump_ModelState_Login()
        {
            var resp = await _client.PostAsJsonAsync("/api/v1/Auth/login", new { });
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            if (body.TryGetProperty("errors", out var errors))
            {
                _output.WriteLine(errors.ToString());
            }
            else
            {
                _output.WriteLine(body.ToString());
            }
        }
    }
}
