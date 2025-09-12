namespace RhSensoWeb.Models
{
    public sealed class PermissionVm
    {
        public string CdSistema { get; set; } = "";
        public string DcSistema { get; set; } = "";
        public string CdGrUser { get; set; } = "";
        public string CdFuncao { get; set; } = "";
        public string CdAcoes { get; set; } = ""; // ex: ACEI
        public char CdRestric { get; set; }       // L / P / C
    }

    // Helpers pra normalizar vinda da API
    public static class PermissionHelpers
    {
        public static string TrimOrEmpty(this string? s)
            => string.IsNullOrWhiteSpace(s) ? "" : s.Trim();

        public static string NormalizeAcoes(this string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            // remove espaços e deixa tudo maiúsculo
            var raw = new string(s.Where(ch => !char.IsWhiteSpace(ch)).ToArray()).ToUpperInvariant();
            // remove duplicados mantendo ordem
            var seen = new HashSet<char>();
            var buf = new System.Text.StringBuilder();
            foreach (var ch in raw)
            {
                if (!seen.Contains(ch))
                {
                    seen.Add(ch);
                    buf.Append(ch);
                }
            }
            return buf.ToString();
        }

        public static char NormalizeRestric(this char c)
        {
            c = char.ToUpperInvariant(c);
            return (c is 'L' or 'P' or 'C') ? c : 'L';
        }
    }
}
