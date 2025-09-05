using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using RhSensoWebApi.Tests.Common;
// AJUSTE: troque os usings/nomes conforme seus repositórios/entidades reais
using RhSensoWebApi.Infrastructure.Data.Repositories;
using RhSensoWebApi.Core.Entities;

namespace RhSensoWebApi.Tests.Infrastructure;

public class UserRepositoryTests
{
    [Fact]
    public async Task GetByUsername_Returns_User_And_Flags()
    {
        await using var prov = new SqliteInMemoryProvider();
        var ctx = prov.Context;

        ctx.Users.Add(new User { Id = 1, CdUsuario = "carlos", FlAtivo = true, FlNaoRecebeEmail = false, DcUsuario = "Carlos" });
        await ctx.SaveChangesAsync();

        var repo = new UserRepository(ctx, NullLogger<UserRepository>.Instance);
        var u = await repo.GetByUsernameAsync("carlos");

        u.Should().NotBeNull();
        u!.CdUsuario.Should().Be("carlos");
        u.FlAtivo.Should().BeTrue();
        u.FlNaoRecebeEmail.Should().BeFalse();
    }
}
