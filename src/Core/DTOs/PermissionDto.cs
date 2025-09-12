namespace RhSensoWebApi.Core.DTOs;

public class PermissionDto
{
    public string CdSistema { get; set; } = string.Empty;
    public string DcSistema { get; set; } = string.Empty;
    public string CdGrUser { get; set; } = string.Empty;
    public string CdFuncao { get; set; } = string.Empty;
    public string CdAcoes { get; set; } = string.Empty;
    public char CdRestric { get; set; } = 'N';
}
