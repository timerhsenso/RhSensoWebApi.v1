using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RhSensoWebApi.Core.Entities;

namespace RhSensoWebApi.Infrastructure.Data.Configurations;

public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        builder.ToTable("usrh1");

        builder.HasKey(x => new { x.CdUsuario, x.CdGrUser, x.CdSistema });

        builder.Property(x => x.CdUsuario)
            .HasColumnName("cdusuario")
            .HasMaxLength(50);

        builder.Property(x => x.CdGrUser)
            .HasColumnName("cdgruser")
            .HasMaxLength(50);

        builder.Property(x => x.CdSistema)
            .HasColumnName("cdsistema")
            .HasMaxLength(10);

        builder.Property(x => x.DtFimVal)
            .HasColumnName("dtfimval");

        // Relacionamentos
        builder.HasOne(x => x.User)
            .WithMany(x => x.UserGroups)
            .HasForeignKey(x => x.CdUsuario);

        builder.HasOne(x => x.System)
            .WithMany(x => x.UserGroups)
            .HasForeignKey(x => x.CdSistema);

        // Índices
        builder.HasIndex(x => new { x.CdUsuario, x.CdSistema });
        builder.HasIndex(x => x.DtFimVal);
    }
}

