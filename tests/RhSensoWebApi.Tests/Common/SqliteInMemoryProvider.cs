using Microsoft.EntityFrameworkCore;
using RhSensoWebApi.Infrastructure.Data.Context;

namespace RhSensoWebApi.Tests.Common;

public sealed class SqliteInMemoryProvider : IAsyncDisposable
{
    public AppDbContext Context { get; }

    public SqliteInMemoryProvider()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"rh_tests_{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
    }
}
