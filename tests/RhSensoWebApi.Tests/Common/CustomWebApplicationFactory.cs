using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RhSensoWebApi.Core.Interfaces;
using RhSensoWebApi.Infrastructure.Data.Context;
using RhSensoWebApi.Tests.Common.Fakes;

namespace RhSensoWebApi.Tests.Common;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(opts => opts.UseInMemoryDatabase("rh_tests"));

            services.RemoveAll<IAuthService>();
            services.AddSingleton<IAuthService, FakeAuthService>();

            using var sp = services.BuildServiceProvider().CreateScope();
            var ctx = sp.ServiceProvider.GetRequiredService<AppDbContext>();
            ctx.Database.EnsureCreated();
        });
    }
}
