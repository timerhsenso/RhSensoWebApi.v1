using System;

namespace RhSensoWebApi.Core.Entities.SEG
{
    public class Usuario
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string? SenhaUser { get; set; }
        public string? NomeImpCheque { get; set; }
        public string Tipo { get; set; } = "1";
        public string? NoMatric { get; set; }
        public int? CdEmpresa { get; set; }
        public int? CdFilial { get; set; }
        public int NoUser { get; set; }
        public string? Email { get; set; }
        public string Ativo { get; set; } = "S";
        public Guid Id { get; set; }
        public string? NormalizedUserName { get; set; }
        public Guid? IdFuncionario { get; set; }
        public string? FlNaoRecebeEmail { get; set; }
    }
}
