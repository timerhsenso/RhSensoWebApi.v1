using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RhSensoWebApi.Core.Entities;

namespace RhSensoWebApi.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("tuse1");
        
        builder.HasKey(x => x.CdUsuario);
        
        builder.Property(x => x.CdUsuario)
            .HasColumnName("cdusuario")
            .HasMaxLength(50)
            .IsRequired();
            
        builder.Property(x => x.DcUsuario)
            .HasColumnName("dcusuario")
            .HasMaxLength(100);
            
        builder.Property(x => x.SenhaUser)
            .HasColumnName("senhauser")
            .HasMaxLength(255);
            
        builder.Property(x => x.NmImpcche)
            .HasColumnName("nmimpcche")
            .HasMaxLength(50);
            
        builder.Property(x => x.TpUsuario)
            .HasColumnName("tpusuario")
            .HasMaxLength(20);
            
        builder.Property(x => x.NoMatric)
            .HasColumnName("nomatric")
            .HasMaxLength(20);
            
        builder.Property(x => x.CdEmpresa)
            .HasColumnName("cdempresa")
            .HasMaxLength(10);
            
        builder.Property(x => x.CdFilial)
            .HasColumnName("cdfilial")
            .HasMaxLength(10);
            
        builder.Property(x => x.NoUser)
            .HasColumnName("nouser")
            .HasMaxLength(50);
            
        builder.Property(x => x.FlAtivo)
            .HasColumnName("flativo")
            .HasConversion(
                v => v ? 'S' : 'N',
                v => v == 'S'
            );
            
        builder.Property(x => x.EmailUsuario)
            .HasColumnName("email_usuario")
            .HasMaxLength(150);
            
        builder.Property(x => x.Id)
            .HasColumnName("id");
            
        builder.Property(x => x.NormalizedUsername)
            .HasColumnName("normalizedusername")
            .HasMaxLength(50);
            
        builder.Property(x => x.IdFuncionario)
            .HasColumnName("idfuncionario");
            
        builder.Property(x => x.FlNaoRecebeEmail)
            .HasColumnName("flnaorecebeemail")
            .HasConversion(
                v => v ? 'S' : 'N',
                v => v == 'S'
            );
        
        // Índices para performance
        builder.HasIndex(x => x.FlAtivo);
        builder.HasIndex(x => x.IdFuncionario);
        builder.HasIndex(x => x.NormalizedUsername);
    }
}

