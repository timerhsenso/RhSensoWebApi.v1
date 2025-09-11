using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RhSensoWebApi.Core.Entities.SEG;

namespace RhSensoWebApi.Infrastructure.Data.Configurations.SEG
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("tuse1");
            builder.HasKey(x => x.CdUsuario);

            builder.Property(x => x.CdUsuario).HasColumnName("cdusuario").HasMaxLength(30).IsRequired();
            builder.Property(x => x.DcUsuario).HasColumnName("dcusuario").HasMaxLength(50).IsRequired();
            builder.Property(x => x.SenhaUser).HasColumnName("senhauser").HasMaxLength(100);
            builder.Property(x => x.NmImpCche).HasColumnName("nmimpcche").HasMaxLength(50);
            builder.Property(x => x.TpUsuario).HasColumnName("tpusuario").HasMaxLength(1);
            builder.Property(x => x.NoMatric).HasColumnName("nomatric").HasMaxLength(8);
            builder.Property(x => x.CdEmpresa).HasColumnName("cdempresa");
            builder.Property(x => x.CdFilial).HasColumnName("cdfilial");
            builder.Property(x => x.NoUser).HasColumnName("nouser").IsRequired();
            builder.Property(x => x.EmailUsuario).HasColumnName("email_usuario").HasMaxLength(100);
            builder.Property(x => x.FlAtivo).HasColumnName("flativo").HasMaxLength(1).IsRequired();

            builder.Property(x => x.Id).HasColumnName("id").IsRequired();

            builder.Property(x => x.NormalizedUserName).HasColumnName("normalizedusername").HasMaxLength(30);

            builder.Property(x => x.IdFuncionario)
                   .HasColumnName("idfuncionario")
                   .HasColumnType("uniqueidentifier"); // <- GUID no banco

            builder.Property(x => x.FlNaoRecebeEmail).HasColumnName("flnaorecebeemail").HasMaxLength(1);
        }
    }
}
