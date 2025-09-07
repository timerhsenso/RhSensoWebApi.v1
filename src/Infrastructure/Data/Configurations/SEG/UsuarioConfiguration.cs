using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RhSensoWebApi.Core.Entities.SEG;

namespace RhSensoWebApi.Infrastructure.Data.Configurations.SEG
{
    public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> b)
        {
            b.ToTable("tuse1", "dbo");
            b.HasKey(x => x.Codigo);
            b.Property(x => x.Codigo).HasColumnName("cdusuario").HasMaxLength(30).IsRequired();
            b.Property(x => x.Descricao).HasColumnName("dcusuario").HasMaxLength(50).IsRequired();
            b.Property(x => x.SenhaUser).HasColumnName("senhauser").HasMaxLength(20);
            b.Property(x => x.NomeImpCheque).HasColumnName("nmimpcche").HasMaxLength(50);
            b.Property(x => x.Tipo).HasColumnName("tpusuario").HasMaxLength(1).IsRequired();
            b.Property(x => x.NoMatric).HasColumnName("nomatric").HasMaxLength(8);
            b.Property(x => x.CdEmpresa).HasColumnName("cdempresa");
            b.Property(x => x.CdFilial).HasColumnName("cdfilial");
            b.Property(x => x.NoUser).HasColumnName("nouser").IsRequired();
            b.Property(x => x.Email).HasColumnName("email_usuario").HasMaxLength(100);
            b.Property(x => x.Ativo).HasColumnName("flativo").HasMaxLength(1).IsRequired();
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.NormalizedUserName).HasColumnName("normalizedusername").HasMaxLength(30);
            b.Property(x => x.IdFuncionario).HasColumnName("idfuncionario");
            b.Property(x => x.FlNaoRecebeEmail).HasColumnName("flnaorecebeemail").HasMaxLength(1);
        }
    }
}
