using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace RhSensoWebApi.Tests
{
    // Usa Program (public partial) da API como entry point
    public class TestApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development"); // garante mesmo pipeline do dev

            builder.ConfigureServices(services =>
            {
                // Adiciona controllers da API e o TestController do projeto de testes
                var mvc = services.AddControllers();

                mvc.PartManager.ApplicationParts.Add(
                    new AssemblyPart(typeof(Program).Assembly)            // API controllers
                );

                mvc.PartManager.ApplicationParts.Add(
                    new AssemblyPart(typeof(TestController).Assembly)     // TestController (dos testes)
                );
            });
        }
    }
}
