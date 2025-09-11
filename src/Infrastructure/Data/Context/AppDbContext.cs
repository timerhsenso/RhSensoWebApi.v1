using Microsoft.EntityFrameworkCore;
using RhSensoWebApi.Core.Entities;
using RhSensoWebApi.Core.Entities.SEG;

namespace RhSensoWebApi.Infrastructure.Data.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ENTIDADES CORRETAS
    public DbSet<Usuario> Usuarios { get; set; } = null!;

    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();
    public DbSet<Sistema> Sistemas => Set<Sistema>();
    public DbSet<Botao> Botoes => Set<Botao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // aplica configurações (ex.: UserGroupConfiguration)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Se você usa soft delete, reabilite a extensão do seu projeto:
        // modelBuilder.ApplySoftDeleteQueryFilters();
    }
}
