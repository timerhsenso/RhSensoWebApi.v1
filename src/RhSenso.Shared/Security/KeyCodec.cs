using System;
using System.Text;

namespace RhSenso.Shared.Security
{
    /// <summary>
    /// Converte "texto cru" em Base64Url (seguro para usar em querystring),
    /// e volta para o texto cru. Ex.: "SEG|USUARIO|EDITAR" -> "U0VHfFVTVU..."
    /// </summary>
    public static class KeyCodec
    {
        public static string ToBase64Url(string raw)
        {
            if (raw is null) return string.Empty;
            var bytes = Encoding.UTF8.GetBytes(raw);
            var s = Convert.ToBase64String(bytes);
            // Base64Url (RFC 4648 §5)
            s = s.TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return s;
        }

        public static string FromBase64Url(string safe)
        {
            if (string.IsNullOrWhiteSpace(safe)) return string.Empty;
            var s = safe.Replace('-', '+').Replace('_', '/');
            // recoloca o padding se necessário
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            var bytes = Convert.FromBase64String(s);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
