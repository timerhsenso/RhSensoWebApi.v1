
using System.Net.Http.Json;
using System.Text.Json;
using RhSenso.Shared.SEG.Botoes;
namespace RhSensoWeb.Services.ApiClients
{
    public class BotoesApi : IBotoesApi
    {
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
        public BotoesApi(HttpClient http) => _http = http;
        public async Task<(int total, List<BotaoListDto> data)> ListAsync(string? sistema, string? funcao, string? search, int page, int pageSize, string orderBy, bool asc, CancellationToken ct = default)
        {
            string url = $"api/v1/botoes?sistema={Uri.EscapeDataString(sistema ?? "")}&funcao={Uri.EscapeDataString(funcao ?? "")}&search={Uri.EscapeDataString(search ?? "")}&page={page}&pageSize={pageSize}&orderBy={orderBy}&asc={asc}";
            using var resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();
            var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            int total = doc.RootElement.GetProperty("total").GetInt32();
            var data = JsonSerializer.Deserialize<List<BotaoListDto>>(doc.RootElement.GetProperty("data").GetRawText(), _json)!;
            return (total, data);
        }
        public Task<BotaoFormDto?> GetAsync(string sistema, string funcao, string nome, CancellationToken ct = default)
            => _http.GetFromJsonAsync<BotaoFormDto>($"api/v1/botoes/{sistema}/{funcao}/{nome}", _json, ct);
        public async Task CreateAsync(BotaoFormDto dto, CancellationToken ct = default)
        {
            var r = await _http.PostAsJsonAsync("api/v1/botoes", dto, _json, ct); r.EnsureSuccessStatusCode();
        }
        public async Task UpdateAsync(string sistema, string funcao, string nome, BotaoFormDto dto, CancellationToken ct = default)
        {
            var r = await _http.PutAsJsonAsync($"api/v1/botoes/{sistema}/{funcao}/{nome}", dto, _json, ct); r.EnsureSuccessStatusCode();
        }
        public async Task DeleteAsync(string sistema, string funcao, string nome, CancellationToken ct = default)
        {
            var r = await _http.DeleteAsync($"api/v1/botoes/{sistema}/{funcao}/{nome}", ct); r.EnsureSuccessStatusCode();
        }
    }
}
