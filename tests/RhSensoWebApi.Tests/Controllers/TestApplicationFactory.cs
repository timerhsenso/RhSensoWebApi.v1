using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RhSensoWebApi.Tests.Controllers
{
    /// <summary>
    /// Sobe a API em memória para os testes de integração.
    /// Mantém ambiente Development e permite customizações de DI no futuro.
    /// </summary>
    public class TestApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            // Se quiser customizar DI para testes, faça aqui:
            // builder.ConfigureServices(services => { ... });
        }
    }
}
