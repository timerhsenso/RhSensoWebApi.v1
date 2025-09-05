using Microsoft.EntityFrameworkCore;
using RhSensoWebApi.Core.Entities;       // User, UserGroup, SystemEntity, GroupPermission
using RhSensoWebApi.Core.Entities.SEG;   // Botao

namespace RhSensoWebApi.Infrastructure.Data.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<SystemEntity> Systems => Set<SystemEntity>();
    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();

    // Adicione o DbSet do módulo de Botões
    public DbSet<Botao> Botoes => Set<Botao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Carrega TODAS as IEntityTypeConfiguration<T> deste assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
