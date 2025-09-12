using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RhSensoWebApi.Core.Entities;

namespace RhSensoWebApi.Infrastructure.Data.Configurations
{
    public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
    {
        public void Configure(EntityTypeBuilder<UserGroup> builder)
        {
            // Tabela correta
            builder.ToTable("usrh1");

            // ✅ Use como chave composta apenas o que existe na entidade:
            // (cdusuario, cdsistema, cdgruser)
            builder.HasKey(x => new { x.CdUsuario, x.CdSistema, x.CdGrUser });

            // Colunas existentes na entidade:
            builder.Property(x => x.CdUsuario)
                   .HasColumnName("cdusuario")
                   .HasMaxLength(30)
                   .IsRequired();

            builder.Property(x => x.CdSistema)
                   .HasColumnName("cdsistema")
                   .HasMaxLength(10); // no DDL é char(10) NULL, então não marco IsRequired

            builder.Property(x => x.CdGrUser)
                   .HasColumnName("cdgruser")
                   .HasMaxLength(30)
                   .IsRequired();

            // Campo que já usamos no filtro (dtfimval IS NULL)
            builder.Property(x => x.DtFimVal)
                   .HasColumnName("dtfimval")
                   .HasColumnType("datetime");

            // FKs opcionais (só se você tem as entidades mapeadas)
            builder.HasOne(x => x.Usuario)
                   .WithMany()
                   .HasForeignKey(x => x.CdUsuario)
                   .HasPrincipalKey(u => u.CdUsuario);

            builder.HasOne(x => x.Sistema)
                   .WithMany()
                   .HasForeignKey(x => x.CdSistema)
                   .HasPrincipalKey(s => s.CdSistema);

            // ❌ Sem mapear DtIniVal/IdUsuario/IdGrupoDeUsuario/Id agora,
            //    porque sua entidade UserGroup não tem essas propriedades ainda.
        }
    }
}
