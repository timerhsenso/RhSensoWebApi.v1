// src/RhSensoWeb/Services/Security/PermissionProvider.cs
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RhSensoWeb.Services.Security
{
    // DTO que representa uma permissão (igual ao da API)
    public sealed class PermissionDto
    {
        public string CdSistema { get; set; } = "";
        public string CdFuncao { get; set; } = "";
        public string CdAcoes { get; set; } = ""; // ex: "ACEI"
        public char CdRestric { get; set; } = 'L';
    }

    // Envelope da resposta da API
    file sealed class ApiBaseResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    public interface IPermissionProvider
    {
        Task<IReadOnlyList<PermissionDto>> GetAsync(CancellationToken ct = default);
        Task<bool> HasFeatureAsync(string sistema, string funcao, CancellationToken ct = default);
        Task<bool> HasActionAsync(string sistema, string funcao, char acao, CancellationToken ct = default);
        Task<char> GetRestricaoAsync(string sistema, string funcao, CancellationToken ct = default);
        Task ReloadAsync(CancellationToken ct = default);
        Task EnsureLoadedAsync(HttpContext http);
    }

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
            if (ctx is null)
            {
                _logger.LogWarning("HttpContext é null - retornando lista vazia");
                return Array.Empty<PermissionDto>();
            }

            // 1) Verifica cache da sessão
            var cached = ctx.Session.GetString(SessionKey);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<PermissionDto>>(
                        cached, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    ) ?? new();
                    _logger.LogInformation("✅ Permissões do cache: {Count} itens", list.Count);
                    return list;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "❌ Erro ao deserializar cache de permissões");
                }
            }

            // 2) Busca na API
            try
            {
                var client = _http.CreateClient("Api");

                // ⚠️ TESTE AMBOS endpoints - primeiro 'permissions', depois 'permissoes'
                HttpResponseMessage? resp = null;
                try
                {
                    resp = await client.GetAsync("api/v1/auth/permissions", ct);
                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Endpoint 'permissions' falhou com {Status}, tentando 'permissoes'",
                            (int)resp.StatusCode);
                        resp.Dispose();
                        resp = await client.GetAsync("api/v1/auth/permissoes", ct);
                    }
                }
                catch
                {
                    // Se 'permissions' der erro, tenta 'permissoes'
                    resp?.Dispose();
                    resp = await client.GetAsync("api/v1/auth/permissoes", ct);
                }

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("❌ API retornou {Status} para ambos endpoints de permissão",
                        (int)resp.StatusCode);
                    return Array.Empty<PermissionDto>();
                }

                // Deserializa o envelope da resposta
                var envelope = await resp.Content.ReadFromJsonAsync<ApiBaseResponse<List<PermissionDto>>>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

                var data = envelope?.Data ?? new List<PermissionDto>();

                // Normaliza e limpa os dados
                foreach (var p in data)
                {
                    p.CdSistema = (p.CdSistema ?? "").Trim().ToUpperInvariant();
                    p.CdFuncao = (p.CdFuncao ?? "").Trim().ToUpperInvariant();
                    p.CdAcoes = (p.CdAcoes ?? "").Trim().ToUpperInvariant();
                    p.CdRestric = char.ToUpperInvariant(p.CdRestric);
                }

                _logger.LogInformation("✅ API retornou {Count} permissões", data.Count);

                // Log das primeiras para debug
                foreach (var p in data.Take(3))
                {
                    _logger.LogInformation("   → {Sistema}/{Funcao}: '{Acoes}' ({Restric})",
                        p.CdSistema, p.CdFuncao, p.CdAcoes, p.CdRestric);
                }

                // Salva na sessão
                ctx.Session.SetString(SessionKey, JsonSerializer.Serialize(data));
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao buscar permissões na API");
                return Array.Empty<PermissionDto>();
            }
        }

        public async Task ReloadAsync(CancellationToken ct = default)
        {
            _httpCtx.HttpContext?.Session.Remove(SessionKey);
            _logger.LogInformation("🔄 Cache de permissões limpo, recarregando...");
            await GetAsync(ct);
        }

        public async Task<bool> HasFeatureAsync(string sistema, string funcao, CancellationToken ct = default)
        {
            var s = (sistema ?? "").Trim().ToUpperInvariant();
            var f = (funcao ?? "").Trim().ToUpperInvariant();

            var list = await GetAsync(ct);
            var hasFeature = list.Any(p => p.CdSistema == s && p.CdFuncao == f);

            _logger.LogInformation("🔍 HasFeature({Sistema}, {Funcao}) = {Result}", s, f, hasFeature);
            return hasFeature;
        }

        public async Task<bool> HasActionAsync(string sistema, string funcao, char acao, CancellationToken ct = default)
        {
            var s = (sistema ?? "").Trim().ToUpperInvariant();
            var f = (funcao ?? "").Trim().ToUpperInvariant();
            var a = char.ToUpperInvariant(acao);

            var list = await GetAsync(ct);
            var permission = list.FirstOrDefault(p => p.CdSistema == s && p.CdFuncao == f);

            if (permission is null)
            {
                _logger.LogInformation("🔍 HasAction({Sistema}, {Funcao}, {Acao}) = FALSE (permissão não encontrada)",
                    s, f, a);
                return false;
            }

            var hasAction = !string.IsNullOrEmpty(permission.CdAcoes) && permission.CdAcoes.Contains(a);
            _logger.LogInformation("🔍 HasAction({Sistema}, {Funcao}, {Acao}) = {Result} (disponível: '{Acoes}')",
                s, f, a, hasAction, permission.CdAcoes);

            return hasAction;
        }

        public async Task<char> GetRestricaoAsync(string sistema, string funcao, CancellationToken ct = default)
        {
            var s = (sistema ?? "").Trim().ToUpperInvariant();
            var f = (funcao ?? "").Trim().ToUpperInvariant();
            var list = await GetAsync(ct);
            var p = list.FirstOrDefault(p => p.CdSistema == s && p.CdFuncao == f);
            var result = p?.CdRestric is char r && r != '\0' ? char.ToUpperInvariant(r) : 'L';

            _logger.LogInformation("🔍 GetRestricao({Sistema}, {Funcao}) = {Result}", s, f, result);
            return result;
        }

        public async Task EnsureLoadedAsync(HttpContext http)
        {
            if (http.Session.GetString(SessionKey) is null)
            {
                _logger.LogInformation("🔄 Cache vazio, carregando permissões...");
                await GetAsync();
            }
        }
    }
}