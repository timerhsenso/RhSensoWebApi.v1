namespace RhSensoWebApi.Core.Entities;

public class User
{
    public string CdUsuario { get; set; } = string.Empty; // PK
    public string DcUsuario { get; set; } = string.Empty;
    public string SenhaUser { get; set; } = string.Empty; // Hash
    public string NmImpcche { get; set; } = string.Empty;
    public string TpUsuario { get; set; } = string.Empty;
    public string NoMatric { get; set; } = string.Empty;
    public string CdEmpresa { get; set; } = string.Empty;
    public string CdFilial { get; set; } = string.Empty;
    public string NoUser { get; set; } = string.Empty;
    public string EmailUsuario { get; set; } = string.Empty;
    public bool FlAtivo { get; set; }
    public int Id { get; set; }
    public string NormalizedUsername { get; set; } = string.Empty;
    public int IdFuncionario { get; set; }
    public bool FlNaoRecebeEmail { get; set; }
    
    // Navigation Properties
    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
}

