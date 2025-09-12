using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RhSensoWeb.Models;

namespace RhSensoWeb.Services.Security
{
    /// <summary>
    /// Busca permissões na API, normaliza, agrega e grava em Session (UserPermissions).
    /// </summary>
    public sealed class PermissionSessionService
    {
        private readonly IHttpClientFactory _http;
        private readonly IHttpContextAccessor _ctx;

        private const string SessionKey = "UserPermissions";

        public PermissionSessionService(IHttpClientFactory http, IHttpContextAccessor ctx)
        {
            _http = http;
            _ctx = ctx;
        }

        // Modelo para ler a resposta da API (/api/v1/auth/permissions)
        private sealed class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public T? Data { get; set; }
        }

        /// <summary>
        /// Chama a API, agrega e grava a lista na Session.
        /// </summary>
        public async Task SyncAsync(CancellationToken ct = default)
        {
            var client = _http.CreateClient("Api"); // já envia Bearer a partir do cookie AuthToken

            using var resp = await client.GetAsync("api/v1/auth/permissions", ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync(ct);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var api = JsonSerializer.Deserialize<ApiResponse<List<PermissionVm>>>(json, options)
                      ?? new ApiResponse<List<PermissionVm>> { Success = false, Data = new() };

            var raw = api.Data ?? new List<PermissionVm>();

            // Normaliza campos (tira espaços, uppercase das ações, etc.)
            var norm = raw.Select(p => new PermissionVm
            {
                CdSistema = p.CdSistema.TrimOrEmpty(),     // "RHU       " => "RHU"
                DcSistema = p.DcSistema.TrimOrEmpty(),
                CdGrUser = p.CdGrUser.TrimOrEmpty(),
                CdFuncao = p.CdFuncao.TrimOrEmpty(),
                CdAcoes = p.CdAcoes.NormalizeAcoes(),   // "AECIP   " => "AECIP"
                CdRestric = p.CdRestric.NormalizeRestric()
            });

            // Agrega por (sistema, funcao): une ações e escolhe restrição mais restritiva (C>P>L)
            var agregada =
                norm.GroupBy(p => new { p.CdSistema, p.CdFuncao })
                    .Select(g =>
                    {
                        // Une ações removendo duplicidades (ordem natural pela primeira ocorrência)
                        var acoes = string.Concat(
                            g.SelectMany(x => x.CdAcoes ?? string.Empty)
                             .Where(ch => !char.IsWhiteSpace(ch))
                             .Distinct()
                        );

                        // Mais restritiva: C > P > L
                        char restr = 'L';
                        if (g.Any(x => x.CdRestric == 'C')) restr = 'C';
                        else if (g.Any(x => x.CdRestric == 'P')) restr = 'P';

                        // Tenta manter um DcSistema "não vazio"
                        var dcSis = g.Select(x => x.DcSistema).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "";

                        return new PermissionVm
                        {
                            CdSistema = g.Key.CdSistema,
                            DcSistema = dcSis,
                            CdFuncao = g.Key.CdFuncao,
                            CdGrUser = "",        // não precisa após agregação
                            CdAcoes = acoes,
                            CdRestric = restr
                        };
                    })
                    .OrderBy(x => x.CdSistema)
                    .ThenBy(x => x.CdFuncao)
                    .ToList();

            var serialized = JsonSerializer.Serialize(agregada);
            _ctx.HttpContext!.Session.SetString(SessionKey, serialized);
        }

        /// <summary>
        /// Lê a lista da Session. Se não houver, retorna lista vazia.
        /// </summary>
        public List<PermissionVm> Get()
        {
            var s = _ctx.HttpContext!.Session.GetString(SessionKey);
            if (string.IsNullOrEmpty(s)) return new();

            try
            {
                var list = JsonSerializer.Deserialize<List<PermissionVm>>(s)
                           ?? new List<PermissionVm>();
                return list;
            }
            catch
            {
                return new();
            }
        }
    }
}
