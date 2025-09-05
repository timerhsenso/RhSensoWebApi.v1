using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RhSensoWebApi.Core.Entities;

namespace RhSensoWebApi.Infrastructure.Data.Configurations;

public class GroupPermissionConfiguration : IEntityTypeConfiguration<GroupPermission>
{
    public void Configure(EntityTypeBuilder<GroupPermission> builder)
    {
        builder.ToTable("hbrh1");

        builder.HasKey(x => new { x.CdGrUser, x.CdSistema, x.CdFuncao });

        builder.Property(x => x.CdGrUser)
            .HasColumnName("cdgruser")
            .HasMaxLength(50);

        builder.Property(x => x.CdSistema)
            .HasColumnName("cdsistema")
            .HasMaxLength(10);

        builder.Property(x => x.CdFuncao)
            .HasColumnName("cdfuncao")
            .HasMaxLength(100);

        builder.Property(x => x.CdAcoes)
            .HasColumnName("cdacoes")
            .HasMaxLength(10);

        builder.Property(x => x.CdRestric)
            .HasColumnName("cdrestric")
            .HasMaxLength(1);

        // Relacionamento
        // Navegação ignorada para evitar FK (CdGrUser, CdSistema) incompatível com PK (CdUsuario, CdGrUser, CdSistema)
        builder.Ignore(x => x.UserGroup);


        // Índices
        builder.HasIndex(x => new { x.CdGrUser, x.CdSistema });
        builder.HasIndex(x => x.CdFuncao);
    }
}

