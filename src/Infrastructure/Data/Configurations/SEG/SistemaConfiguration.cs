using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RhSensoWebApi.Core.Entities.SEG;

namespace RhSensoWebApi.Infrastructure.Data.Configurations.SEG
{
    public class SistemaConfiguration : IEntityTypeConfiguration<Sistema>
    {
        public void Configure(EntityTypeBuilder<Sistema> builder)
        {
            builder.ToTable("tsistema");

            builder.HasKey(x => x.CdSistema);

            builder.Property(x => x.CdSistema)
                   .HasColumnName("cdsistema")
                   .HasColumnType("char(10)")
                   .IsFixedLength()
                   .HasMaxLength(10)
                   .IsRequired();

            builder.Property(x => x.DcSistema)
                   .HasColumnName("dcsistema")
                   .HasColumnType("varchar(60)")
                   .HasMaxLength(60)
                   .IsRequired();

            builder.Property(x => x.Ativo)
                   .HasColumnName("ativo")
                   .HasColumnType("bit")
                   .IsRequired();
        }
    }
}
