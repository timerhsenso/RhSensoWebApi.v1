using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RhSensoWebApi.Core.Entities.SEG;

namespace RhSensoWebApi.Infrastructure.Data.Configurations.SEG
{
    public sealed class BotaoConfiguration : IEntityTypeConfiguration<Botao>
    {
        public void Configure(EntityTypeBuilder<Botao> e)
        {
            // se usa schema, troque por e.ToTable("btfuncao", "dbo");
            e.ToTable("btfuncao");

            e.HasKey(x => new { x.CodigoSistema, x.CodigoFuncao, x.Nome });

            e.Property(x => x.CodigoSistema)
                .HasColumnName("cdsistema")
                .HasMaxLength(10)
                .IsFixedLength();

            e.Property(x => x.CodigoFuncao)
                .HasColumnName("cdfuncao")
                .HasMaxLength(30);

            e.Property(x => x.Nome)
                .HasColumnName("nmbotao")
                .HasMaxLength(30);

            e.Property(x => x.Descricao)
                .HasColumnName("dcbotao")
                .HasMaxLength(60)
                .IsRequired();

            e.Property(x => x.Acao)
                .HasColumnName("cdacao")
                .HasMaxLength(1)
                .IsRequired();
        }
    }
}
