using System.Text.Json;

namespace RhSensoWeb.Services.ApiClients;

public interface IBotoesGateway
{
    Task<PagedResult<BotaoDto>> SearchAsync(
        string? codigoSistema,
        string? codigoFuncao,
        int page,
        int pageSize,
        string? orderBy,
        bool? asc,
        string? q,
        CancellationToken ct);
}

public sealed class BotoesGateway : IBotoesGateway
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BotoesGateway(HttpClient http) => _http = http;

    public async Task<PagedResult<BotaoDto>> SearchAsync(
        string? codigoSistema,
        string? codigoFuncao,
        int page,
        int pageSize,
        string? orderBy,
        bool? asc,
        string? q,
        CancellationToken ct)
    {
        var qs = new List<string>();

        if (!string.IsNullOrWhiteSpace(codigoSistema))
            qs.Add($"codigoSistema={Uri.EscapeDataString(codigoSistema)}");

        if (!string.IsNullOrWhiteSpace(codigoFuncao))
            qs.Add($"codigoFuncao={Uri.EscapeDataString(codigoFuncao)}");

        if (!string.IsNullOrWhiteSpace(orderBy))
            qs.Add($"orderBy={Uri.EscapeDataString(orderBy)}");

        if (asc.HasValue)
            qs.Add($"asc={asc.Value.ToString().ToLowerInvariant()}");

        qs.Add($"page={page}");
        qs.Add($"pageSize={pageSize}");

        if (!string.IsNullOrWhiteSpace(q))
            qs.Add($"q={Uri.EscapeDataString(q)}");

        var url = "api/v1/botoes";
        if (qs.Count > 0) url += "?" + string.Join("&", qs);

        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var data = await JsonSerializer.DeserializeAsync<PagedResult<BotaoDto>>(stream, _json, ct);

        return data ?? new PagedResult<BotaoDto> { Total = 0, Data = [] };
    }
}

/* === DTOs simples pro deserializador === */
public sealed class PagedResult<T>
{
    public int Total { get; set; }
    public List<T> Data { get; set; } = new();
}

public sealed class BotaoDto
{
    public Guid Id { get; set; }
    public string? CodigoSistema { get; set; }
    public string? CodigoFuncao { get; set; }
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public bool? Ativo { get; set; }
}
