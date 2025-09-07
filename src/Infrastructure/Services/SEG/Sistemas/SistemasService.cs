using Microsoft.EntityFrameworkCore;
using RhSenso.Shared.SEG.Sistemas;
using RhSensoWebApi.Core.Abstractions.SEG.Sistemas;
using RhSensoWebApi.Core.Entities.SEG;
using RhSensoWebApi.Infrastructure.Data.Context; // AppDbContext

namespace RhSensoWebApi.Infrastructure.Services.SEG.Sistemas
{
    public sealed class SistemasService : ISistemasService
    {
        private readonly AppDbContext _db;
        public SistemasService(AppDbContext db) => _db = db;

        public async Task<List<SistemaListDto>> GetAllAsync(CancellationToken ct = default)
            => await _db.Sistemas.AsNoTracking()
                .OrderBy(s => s.Descricao)
                .Select(s => new SistemaListDto(s.CdSistema, s.Descricao))
                .ToListAsync(ct);

        public async Task<SistemaListDto?> GetByIdAsync(string codigo, CancellationToken ct = default)
        {
            var s = await _db.Sistemas.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CdSistema == codigo, ct);
            return s is null ? null : new SistemaListDto(s.CdSistema, s.Descricao);
        }

        public async Task CreateAsync(SistemaCreateDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Codigo) || dto.Codigo.Length > 10)
                throw new ArgumentException("Código inválido (até 10 chars).");
            if (string.IsNullOrWhiteSpace(dto.Descricao) || dto.Descricao.Length > 60)
                throw new ArgumentException("Descrição inválida (até 60 chars).");

            var exists = await _db.Sistemas.AnyAsync(x => x.CdSistema == dto.Codigo, ct);
            if (exists) throw new InvalidOperationException("Código já existente.");

            _db.Sistemas.Add(new Sistema
            {
                CdSistema = dto.Codigo.Trim(),
                Descricao = dto.Descricao.Trim(),
                Ativo = true
            });
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(string codigo, SistemaUpdateDto dto, CancellationToken ct = default)
        {
            var s = await _db.Sistemas.FirstOrDefaultAsync(x => x.CdSistema == codigo, ct)
                    ?? throw new KeyNotFoundException("Sistema não encontrado.");

            if (string.IsNullOrWhiteSpace(dto.Descricao) || dto.Descricao.Length > 60)
                throw new ArgumentException("Descrição inválida (até 60 chars).");

            s.Descricao = dto.Descricao.Trim();
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(string codigo, CancellationToken ct = default)
        {
            var s = await _db.Sistemas.FirstOrDefaultAsync(x => x.CdSistema == codigo, ct)
                    ?? throw new KeyNotFoundException("Sistema não encontrado.");

            _db.Sistemas.Remove(s);
            await _db.SaveChangesAsync(ct);
        }
    }
}
