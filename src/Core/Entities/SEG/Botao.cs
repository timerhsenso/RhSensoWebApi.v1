
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RhSensoWebApi.Core.Entities.SEG
{
    [Table("btfuncao", Schema = "dbo")]
    public partial class Botao
    {
        [Key, Column("cdsistema", TypeName = "char(10)")]
        public string CodigoSistema { get; set; } = string.Empty;
        [Key, Column("cdfuncao", TypeName = "varchar(30)")]
        public string CodigoFuncao { get; set; } = string.Empty;
        [Key, Column("nmbotao", TypeName = "varchar(30)")]
        public string Nome { get; set; } = string.Empty;
        [Required, MaxLength(60)]
        [Column("dcbotao", TypeName = "varchar(60)")]
        public string Descricao { get; set; } = string.Empty;
        [Required, MaxLength(1)]
        [Column("cdacao", TypeName = "char(1)")]
        public string Acao { get; set; } = string.Empty;
    }
}
