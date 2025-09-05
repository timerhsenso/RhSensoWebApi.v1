
using RhSenso.Shared.SEG.Botoes;
namespace RhSensoWeb.Services.ApiClients
{
    public interface IBotoesApi
    {
        Task<(int total, List<BotaoListDto> data)> ListAsync(string? sistema, string? funcao, string? search, int page, int pageSize, string orderBy, bool asc, CancellationToken ct = default);
        Task<BotaoFormDto?> GetAsync(string sistema, string funcao, string nome, CancellationToken ct = default);
        Task CreateAsync(BotaoFormDto dto, CancellationToken ct = default);
        Task UpdateAsync(string sistema, string funcao, string nome, BotaoFormDto dto, CancellationToken ct = default);
        Task DeleteAsync(string sistema, string funcao, string nome, CancellationToken ct = default);
    }
}
