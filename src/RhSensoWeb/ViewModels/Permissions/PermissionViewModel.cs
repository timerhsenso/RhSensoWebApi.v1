namespace RhSensoWeb.ViewModels.Permissions
{
    public sealed class PermissionViewModel
    {
        public string CdSistema { get; set; } = string.Empty;
        public string DcSistema { get; set; } = string.Empty;
        public string CdGrUser { get; set; } = string.Empty;

        public string PermissionCode { get; set; } = string.Empty; // CdPermissao
        public string PermissionName { get; set; } = string.Empty; // DcPermissao

        public string CdFuncao { get; set; } = string.Empty;
        public string CdAcoes { get; set; } = string.Empty; // A,C,E,I etc.
        public char CdRestric { get; set; } = 'N';
    }
}
