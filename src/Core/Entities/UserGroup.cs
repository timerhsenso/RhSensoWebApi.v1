namespace RhSensoWebApi.Core.Entities;

public class UserGroup
{
    public string CdUsuario { get; set; } = string.Empty; // FK
    public string CdGrUser { get; set; } = string.Empty;
    public string CdSistema { get; set; } = string.Empty; // FK
    public DateTime? DtFimVal { get; set; }
    
    // Navigation Properties
    public User User { get; set; } = null!;
    public SystemEntity System { get; set; } = null!;
    public ICollection<GroupPermission> GroupPermissions { get; set; } = new List<GroupPermission>();
}

