using RhSenso.Shared.SEG.Sistemas;

namespace RhSensoWebApi.Core.Abstractions.SEG.Sistemas
{
    public interface ISistemasService
    {
        Task<List<SistemaListDto>> GetAllAsync(CancellationToken ct = default);
        Task<SistemaListDto?> GetByIdAsync(string codigo, CancellationToken ct = default);
        Task CreateAsync(SistemaCreateDto dto, CancellationToken ct = default);
        Task UpdateAsync(string codigo, SistemaUpdateDto dto, CancellationToken ct = default);
        Task DeleteAsync(string codigo, CancellationToken ct = default);
    }
}
