using RhSenso.Shared.SEG.Botoes;

namespace RhSensoWebApi.Core.Abstractions.SEG.Botoes
{
    public interface IBotoesService
    {
        Task<(IEnumerable<BotaoListDto> Data, int Total)> ListAsync(string? sistema, string? funcao, string? search, int page, int pageSize, string? orderBy, bool asc);
        Task<BotaoFormDto?> GetAsync(string sistema, string funcao, string nome);
        Task CreateAsync(BotaoFormDto dto);
        Task UpdateAsync(string sistema, string funcao, string nome, BotaoFormDto dto);
        Task DeleteAsync(string sistema, string funcao, string nome);
    }
}
