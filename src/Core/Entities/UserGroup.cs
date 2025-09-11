using RhSensoWebApi.Core.Entities.SEG;

namespace RhSensoWebApi.Core.Entities;

public class UserGroup
{
    public string CdUsuario { get; set; } = string.Empty; // FK -> Usuario.CdUsuario
    public string CdSistema { get; set; } = string.Empty; // FK -> Sistema.CdSistema
    public string CdGrUser { get; set; } = string.Empty;

    public DateTime? DtFimVal { get; set; }

    // Navegações
    public Usuario? Usuario { get; set; }
    public Sistema? Sistema { get; set; }

    public ICollection<GroupPermission> GroupPermissions { get; set; } = new List<GroupPermission>();
}
