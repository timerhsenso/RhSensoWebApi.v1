using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RhSensoWebApi.Core.Entities;

namespace RhSensoWebApi.Infrastructure.Data.Configurations;

public class SystemConfiguration : IEntityTypeConfiguration<SystemEntity>
{
    public void Configure(EntityTypeBuilder<SystemEntity> builder)
    {
        builder.ToTable("tsistema");

        builder.HasKey(x => x.CdSistema);

        builder.Property(x => x.CdSistema)
            .HasColumnName("cdsistema")
            .HasMaxLength(10);

        builder.Property(x => x.Nome)
            .HasColumnName("nome")
            .HasMaxLength(100);

        builder.Property(x => x.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(255);

        builder.Property(x => x.Ativo)
            .HasColumnName("ativo")
            .HasConversion(
                v => v ? 1 : 0,
                v => v == 1
            );

        builder.HasIndex(x => x.Ativo);
    }
}

