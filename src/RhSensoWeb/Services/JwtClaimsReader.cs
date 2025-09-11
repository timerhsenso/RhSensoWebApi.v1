using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace RhSensoWeb.Services
{
    /// <summary>
    /// Lê o payload do JWT sem validar assinatura (a API já valida).
    /// Converte campos comuns em Claims para o cookie de autenticação da UI.
    /// </summary>
    public static class JwtClaimsReader
    {
        public static IEnumerable<Claim> FromJwt(string jwt)
        {
            var claims = new List<Claim>();

            var parts = jwt.Split('.');
            if (parts.Length < 2)
                return claims;

            try
            {
                var payload = parts[1];
                var json = Encoding.UTF8.GetString(Base64UrlDecode(payload));
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // helpers locais (sem yield)
                string? GetStr(string name)
                    => root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

                List<string> GetMany(params string[] names)
                {
                    var list = new List<string>();
                    foreach (var n in names)
                    {
                        if (!root.TryGetProperty(n, out var p)) continue;

                        if (p.ValueKind == JsonValueKind.String)
                        {
                            var v = p.GetString();
                            if (!string.IsNullOrWhiteSpace(v)) list.Add(v!);
                        }
                        else if (p.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in p.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.String)
                                {
                                    var v = item.GetString();
                                    if (!string.IsNullOrWhiteSpace(v)) list.Add(v!);
                                }
                            }
                        }
                    }
                    return list;
                }

                // subject / id
                var sub = GetStr("sub") ?? GetStr(ClaimTypes.NameIdentifier) ?? GetStr("nameid");
                if (!string.IsNullOrWhiteSpace(sub))
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, sub!));

                // name / email
                var name = GetStr("name") ?? GetStr("unique_name");
                if (!string.IsNullOrWhiteSpace(name))
                    claims.Add(new Claim(ClaimTypes.Name, name!));

                var email = GetStr("email");
                if (!string.IsNullOrWhiteSpace(email))
                    claims.Add(new Claim(ClaimTypes.Email, email!));

                // roles
                foreach (var role in GetMany("role", "roles"))
                    claims.Add(new Claim(ClaimTypes.Role, role));

                // groups
                foreach (var grp in GetMany("groups", "grp"))
                    claims.Add(new Claim(ClaimTypes.GroupSid, grp));

                // permissions
                foreach (var perm in GetMany("perm", "perms", "permissions", "permission")) 
                    claims.Add(new Claim("perm", perm));

                // exp (opcional, útil para UI)
                var expStr = GetStr("exp");
                if (long.TryParse(expStr, out var exp))
                    claims.Add(new Claim("exp", exp.ToString()));
            }
            catch
            {
                // silencioso: se der erro de parse, retornamos as claims já acumuladas (possivelmente vazio)
            }

            return claims;
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string s = input.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            return Convert.FromBase64String(s);
        }
    }
}
