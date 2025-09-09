using Microsoft.EntityFrameworkCore;
using RhSensoWebApi.Core.Entities;
using RhSensoWebApi.Core.Entities.SEG;
using RhSensoWebApi.Infrastructure.Data; // <- extensão ApplySoftDeleteQueryFilters()

namespace RhSensoWebApi.Infrastructure.Data.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<GroupPermission> GroupPermissions => Set<GroupPermission>();

    // *** ÚNICO DbSet correto ***
    public DbSet<Sistema> Sistemas => Set<Sistema>();
    public DbSet<Botao> Botoes => Set<Botao>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica os mapeamentos do assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // 🔹 Item 6 — Filtro global de soft-delete:
        // aplica e => !e.IsDeleted automaticamente para entidades que implementam ISoftDeleteEntity.
        modelBuilder.ApplySoftDeleteQueryFilters();
    }
}
