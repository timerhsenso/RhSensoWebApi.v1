using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RhSensoWebApi.Core.Entities.SEG;

namespace RhSensoWebApi.Infrastructure.Data.Configurations.SEG
{
    public sealed class SistemaConfiguration : IEntityTypeConfiguration<Sistema>
    {
        public void Configure(EntityTypeBuilder<Sistema> b)
        {
            b.ToTable("tsistema", "dbo");
            b.HasKey(x => x.CdSistema);

            b.Property(x => x.CdSistema)
             .HasColumnName("cdsistema")
             .HasColumnType("char(10)")
             .IsRequired();

            b.Property(x => x.Descricao)
             .HasColumnName("dcsistema")
             .HasMaxLength(60) // mude p/ 255 se sua tabela permitir
             .IsRequired();

            b.Property(x => x.Ativo)
             .HasColumnName("flativo")
             .HasColumnType("bit")
             .HasDefaultValue(true);
        }
    }
}
