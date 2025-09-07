namespace RhSensoWebApi.Core.Entities.SEG
{
    /// <summary>dbo.tsistema</summary>
    public class Sistema
    {
        public string CdSistema { get; set; } = string.Empty;  // PK char(10)
        public string Descricao { get; set; } = string.Empty;  // dcsistema (60..255)
        public bool Ativo { get; set; } = true;          // se existir a coluna

        // Navegações usadas em Includes/relacionamentos existentes
        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
        public ICollection<GroupPermission> GroupPermissions { get; set; } = new List<GroupPermission>();
    }
}
