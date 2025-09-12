// src/RhSensoWeb/Services/Security/PermissionProvider.cs
using System.Net.Http.Json;             // <- necessário para ReadFromJsonAsync
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RhSensoWeb.Services.Security
{
    // --- DTO que vem da API ---------------------------------------------------
    public sealed class PermissionDto
    {
        public string CdSistema { get; set; } = "";
        public string DcSistema { get; set; } = "";
        public string CdGrUser { get; set; } = "";
        public string CdFuncao { get; set; } = "";
        public string CdAcoes { get; set; } = ""; // letras: A C E I P R S ...
        public char CdRestric { get; set; } = 'N';
    }

    // Envelope padrão da API (success/message/data)
    file sealed class ApiBaseResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    // --- Contrato do serviço ---------------------------------------------------
    public interface IPermissionProvider
    {
        Task<IReadOnlyList<PermissionDto>> GetAsync(CancellationToken ct = default);
        Task<bool> HasFeatureAsync(string sistema, string funcao, CancellationToken ct = default);
        Task<bool> HasActionAsync(string sistema, string funcao, char acao, CancellationToken ct = default);
        Task<char> GetRestricaoAsync(string sistema, string funcao, CancellationToken ct = default);
        Task ReloadAsync(CancellationToken ct = default);
        Task EnsureLoadedAsync(HttpContext http); // helper para TagHelpers
    }

    // --- Implementação ---------------------------------------------------------
    public sealed class PermissionProvider : IPermissionProvider
    {
        private const string SessionKey = "UserPermissionsJson";

        private readonly IHttpClientFactory _http;
        private readonly IHttpContextAccessor _httpCtx;
        private readonly ILogger<PermissionProvider> _logger;

        public PermissionProvider(
            IHttpClientFactory http,
            IHttpContextAccessor httpCtx,
            ILogger<PermissionProvider> logger)
        {
            _http = http;
            _httpCtx = httpCtx;
            _logger = logger;
        }

        public async Task<IReadOnlyList<PermissionDto>> GetAsync(CancellationToken ct = default)
        {
            var ctx = _httpCtx.HttpContext;
            if (ctx is null) return Array.Empty<PermissionDto>();

            // 1) Tenta sessão (cache)
            var cached = ctx.Session.GetString(SessionKey);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<PermissionDto>>(
                        cached, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new();
                    return list;
                }
                catch { /* parse falhou? segue para API */ }
            }

            // 2) Busca na API
            var client = _http.CreateClient("Api"); // BaseAddress + AuthTokenHandler
            var resp = await client.GetAsync("api/v1/auth/permissions", ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("GET /auth/permissions => {Status}", (int)resp.StatusCode);
                return Array.Empty<PermissionDto>();
            }

            var envelope = await resp.Content.ReadFromJsonAsync<ApiBaseResponse<List<PermissionDto>>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

            var data = envelope?.Data ?? new List<PermissionDto>();

            // normaliza (trim/upper) para facilitar comparação
            foreach (var p in data)
            {
                p.CdSistema = (p.CdSistema ?? "").Trim().ToUpperInvariant();
                p.CdFuncao = (p.CdFuncao ?? "").Trim().ToUpperInvariant();
                p.CdAcoes = (p.CdAcoes ?? "").Trim().ToUpperInvariant();
            }

            // salva em sessão
            ctx.Session.SetString(SessionKey, JsonSerializer.Serialize(data));

            return data;
        }

        public async Task ReloadAsync(CancellationToken ct = default)
        {
            _httpCtx.HttpContext?.Session.Remove(SessionKey);
            await GetAsync(ct);
        }

        public async Task<bool> HasFeatureAsync(string sistema, string funcao, CancellationToken ct = default)
        {
            var s = (sistema ?? "").Trim().ToUpperInvariant();
            var f = (funcao ?? "").Trim().ToUpperInvariant();
            var list = await GetAsync(ct);
            return list.Any(p => p.CdSistema == s && p.CdFuncao == f);
        }

        public async Task<bool> HasActionAsync(string sistema, string funcao, char acao, CancellationToken ct = default)
        {
            var s = (sistema ?? "").Trim().ToUpperInvariant();
            var f = (funcao ?? "").Trim().ToUpperInvariant();
            var a = char.ToUpperInvariant(acao);

            var list = await GetAsync(ct);
            var p = list.FirstOrDefault(p => p.CdSistema == s && p.CdFuncao == f);
            if (p is null) return false;

            // CdAcoes é uma string de letras (ex.: "ACEI"). Basta verificar se contém a letra pedida.
            return !string.IsNullOrEmpty(p.CdAcoes) && p.CdAcoes.Contains(a);
        }

        public async Task<char> GetRestricaoAsync(string sistema, string funcao, CancellationToken ct = default)
        {
            var s = (sistema ?? "").Trim().ToUpperInvariant();
            var f = (funcao ?? "").Trim().ToUpperInvariant();
            var list = await GetAsync(ct);
            var p = list.FirstOrDefault(p => p.CdSistema == s && p.CdFuncao == f);
            return p?.CdRestric is char r && r != '\0' ? char.ToUpperInvariant(r) : 'N';
        }

        // Usado pelos TagHelpers para garantir que o cache esteja populado.
        public async Task EnsureLoadedAsync(HttpContext http)
        {
            if (http.Session.GetString(SessionKey) is null)
                await GetAsync();
        }
    }
}
