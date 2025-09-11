using System;

namespace RhSensoWebApi.Core.Entities.SEG
{
    public class Usuario
    {
        public string CdUsuario { get; set; } = string.Empty;
        public string DcUsuario { get; set; } = string.Empty;
        public string? SenhaUser { get; set; }
        public string? NmImpCche { get; set; }   // <- existe
        public string? TpUsuario { get; set; }

        public string? NoMatric { get; set; }
        public int? CdEmpresa { get; set; }
        public int? CdFilial { get; set; }

        public int NoUser { get; set; }
        public string? EmailUsuario { get; set; }
        public string FlAtivo { get; set; } = "S";

        public Guid Id { get; set; }
        public string? NormalizedUserName { get; set; }
        public Guid? IdFuncionario { get; set; }
        public string? FlNaoRecebeEmail { get; set; }
    }
}
