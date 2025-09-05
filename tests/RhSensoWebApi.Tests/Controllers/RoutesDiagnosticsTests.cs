using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace RhSensoWebApi.Tests.Integration
{
    public class RoutesDiagnosticsTests : IClassFixture<TestApplicationFactory>
    {
        private readonly TestApplicationFactory _factory;
        private readonly ITestOutputHelper _output;

        public RoutesDiagnosticsTests(TestApplicationFactory factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
        }

        [Fact]
        public void Dump_All_Endpoints()
        {
            using var scope = _factory.Services.CreateScope();
            var dataSource = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();

            var routes = dataSource.Endpoints
                .OfType<RouteEndpoint>()
                .Select(e => e.RoutePattern.RawText)
                .OrderBy(x => x)
                .ToList();

            foreach (var r in routes)
                _output.WriteLine(r);

            Assert.NotEmpty(routes); // pelo menos 1 rota
        }
    }
}
