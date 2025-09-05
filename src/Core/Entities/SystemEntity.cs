namespace RhSensoWebApi.Core.Entities;

public class SystemEntity
{
    public string CdSistema { get; set; } = string.Empty; // PK
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Ativo { get; set; }

    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
}

