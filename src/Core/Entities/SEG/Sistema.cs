using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhSensoWebApi.Core.Entities.SEG
{
    [Table("tsistema")]
    public class Sistema
    {
        [Key]
        [Column("cdsistema", TypeName = "char(10)")]
        [MaxLength(10)]
        public string CdSistema { get; set; } = string.Empty;

        [Column("dcsistema", TypeName = "varchar(60)")]
        [MaxLength(60)]
        public string DcSistema { get; set; } = string.Empty;

        [Column("ativo", TypeName = "bit")]
        public bool Ativo { get; set; } = true;
    }
}
