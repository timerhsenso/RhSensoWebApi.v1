using Microsoft.EntityFrameworkCore;
using RhSenso.Shared.SEG.Botoes;
using RhSensoWebApi.Core.Abstractions.SEG.Botoes;
using RhSensoWebApi.Core.Entities.SEG;
using RhSensoWebApi.Infrastructure.Data.Context; // <<< aqui

namespace RhSensoWebApi.Infrastructure.Services.SEG.Botoes // se usar uma pasta Services/SEG/Botoes
{
    public sealed class BotoesService : IBotoesService
    {
        private readonly AppDbContext _db;                     // <<< aqui
        public BotoesService(AppDbContext db) => _db = db;     // <<< aqui

        public async Task<(IEnumerable<BotaoListDto> Data, int Total)> ListAsync(
            string? sistema, string? funcao, string? search, int page, int pageSize, string? orderBy, bool asc)
        {
            // Você pode usar _db.Botoes (temos o DbSet) ou _db.Set<Botao>()
            var q = _db.Botoes.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(sistema)) q = q.Where(x => x.CodigoSistema == sistema);
            if (!string.IsNullOrWhiteSpace(funcao)) q = q.Where(x => x.CodigoFuncao == funcao);
            if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.Nome.Contains(search) || x.Descricao.Contains(search) || x.Acao.Contains(search));

            q = (orderBy?.ToLower()) switch
            {
                "codigosistema" => asc ? q.OrderBy(x => x.CodigoSistema) : q.OrderByDescending(x => x.CodigoSistema),
                "codigofuncao" => asc ? q.OrderBy(x => x.CodigoFuncao) : q.OrderByDescending(x => x.CodigoFuncao),
                "nome" => asc ? q.OrderBy(x => x.Nome) : q.OrderByDescending(x => x.Nome),
                "descricao" => asc ? q.OrderBy(x => x.Descricao) : q.OrderByDescending(x => x.Descricao),
                "acao" => asc ? q.OrderBy(x => x.Acao) : q.OrderByDescending(x => x.Acao),
                _ => asc ? q.OrderBy(x => x.CodigoSistema).ThenBy(x => x.CodigoFuncao).ThenBy(x => x.Nome)
                                      : q.OrderByDescending(x => x.CodigoSistema).ThenByDescending(x => x.CodigoFuncao).ThenByDescending(x => x.Nome),
            };

            var total = await q.CountAsync();
            var data = await q.Skip((page - 1) * pageSize).Take(pageSize)
                               .Select(x => new BotaoListDto(x.CodigoSistema, x.CodigoFuncao, x.Nome, x.Descricao, x.Acao))
                               .ToListAsync();

            return (data, total);
        }

        public async Task<BotaoFormDto?> GetAsync(string sistema, string funcao, string nome)
        {
            var e = await _db.Botoes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CodigoSistema == sistema && x.CodigoFuncao == funcao && x.Nome == nome);
            return e is null ? null : new BotaoFormDto
            {
                CodigoSistema = e.CodigoSistema,
                CodigoFuncao = e.CodigoFuncao,
                Nome = e.Nome,
                Descricao = e.Descricao,
                Acao = e.Acao
            };
        }

        public async Task CreateAsync(BotaoFormDto dto)
        {
            var exists = await _db.Botoes.AnyAsync(x =>
                x.CodigoSistema == dto.CodigoSistema && x.CodigoFuncao == dto.CodigoFuncao && x.Nome == dto.Nome);
            if (exists) throw new InvalidOperationException("Botão já existe.");

            _db.Botoes.Add(new Botao
            {
                CodigoSistema = dto.CodigoSistema.Trim(),
                CodigoFuncao = dto.CodigoFuncao.Trim(),
                Nome = dto.Nome.Trim(),
                Descricao = dto.Descricao.Trim(),
                Acao = (dto.Acao ?? string.Empty).Trim()
            });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(string sistema, string funcao, string nome, BotaoFormDto dto)
        {
            var e = await _db.Botoes.FirstOrDefaultAsync(x => x.CodigoSistema == sistema && x.CodigoFuncao == funcao && x.Nome == nome)
                ?? throw new KeyNotFoundException("Botão não encontrado.");

            e.Descricao = dto.Descricao.Trim();
            e.Acao = (dto.Acao ?? string.Empty).Trim();
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(string sistema, string funcao, string nome)
        {
            var e = await _db.Botoes.FirstOrDefaultAsync(x => x.CodigoSistema == sistema && x.CodigoFuncao == funcao && x.Nome == nome)
                ?? throw new KeyNotFoundException("Botão não encontrado.");

            _db.Botoes.Remove(e);
            await _db.SaveChangesAsync();
        }
    }
}
