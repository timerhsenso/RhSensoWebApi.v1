using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RhSensoWebApi.Core.Entities;

namespace RhSensoWebApi.Infrastructure.Data.Configurations;

public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        builder.ToTable("tgrus1");

        builder.HasKey(x => new { x.CdUsuario, x.CdSistema, x.CdGrUser });

        builder.Property(x => x.CdUsuario).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CdSistema).HasMaxLength(10).IsRequired();
        builder.Property(x => x.CdGrUser).HasMaxLength(10).IsRequired();

        builder.Property(x => x.DtFimVal).HasColumnType("datetime");

        // FK correta para Usuario (sem pedir coleção em Usuario)
        builder
            .HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.CdUsuario)
            .HasPrincipalKey(u => u.CdUsuario);

        // (opcional) FK para Sistema, se desejar navegação
        builder
            .HasOne(x => x.Sistema)
            .WithMany()
            .HasForeignKey(x => x.CdSistema)
            .HasPrincipalKey(s => s.CdSistema);
    }
}
