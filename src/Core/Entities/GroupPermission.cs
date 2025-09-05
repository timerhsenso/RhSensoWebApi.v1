namespace RhSensoWebApi.Core.Entities;

public class GroupPermission
{
    public string CdGrUser { get; set; } = string.Empty; // FK
    public string CdSistema { get; set; } = string.Empty; // FK
    public string CdFuncao { get; set; } = string.Empty;
    public string CdAcoes { get; set; } = string.Empty; // ACEI
    public char CdRestric { get; set; } // L, P, C

    public UserGroup UserGroup { get; set; } = null!;
}

