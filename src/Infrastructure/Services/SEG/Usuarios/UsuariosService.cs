using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RhSenso.Shared.SEG.Usuarios;
using RhSensoWebApi.Core.Abstractions.SEG.Usuarios;
using RhSensoWebApi.Core.Entities.SEG;
using RhSensoWebApi.Infrastructure.Data.Context;

namespace RhSensoWebApi.Infrastructure.Services.SEG.Usuarios
{
    public sealed class UsuariosService : IUsuariosService
    {
        private readonly AppDbContext _db;
        public UsuariosService(AppDbContext db) => _db = db;

        private static string TipoToStr(string tipo) => tipo == "0" ? "Prestador de Serviço" : tipo == "1" ? "Empregado" : $"Tipo {tipo}";
        private static string AtivoToSituacao(string ativo) => string.Equals(ativo, "S", StringComparison.OrdinalIgnoreCase) ? "Ativo" : "Inativo";

        public async Task<List<UsuarioListDto>> GetAllAsync(bool exibirInativos, CancellationToken ct = default)
        {
            var q = _db.Usuarios.AsNoTracking();
            if (!exibirInativos) q = q.Where(x => x.Ativo == "S");
            return await q
                .OrderBy(x => x.Codigo).ThenBy(x => x.Descricao)
                .Select(x => new UsuarioListDto(x.Codigo, x.Descricao, TipoToStr(x.Tipo), x.Email, AtivoToSituacao(x.Ativo), x.NoUser, x.CdEmpresa, x.CdFilial))
                .ToListAsync(ct);
        }

        public async Task<UsuarioListDto?> GetByIdAsync(string codigo, CancellationToken ct = default)
        {
            var x = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Codigo == codigo, ct);
            return x is null ? null : new UsuarioListDto(x.Codigo, x.Descricao, TipoToStr(x.Tipo), x.Email, AtivoToSituacao(x.Ativo), x.NoUser, x.CdEmpresa, x.CdFilial);
        }

        public async Task CreateAsync(UsuarioCreateDto dto, CancellationToken ct = default)
        {
            Validate(dto);
            if (await _db.Usuarios.AnyAsync(x => x.Codigo == dto.Codigo, ct)) throw new InvalidOperationException("Login já existente.");
            var e = new Usuario {
                Codigo = dto.Codigo.Trim().ToUpperInvariant(),
                Descricao = dto.Descricao.Trim(),
                Tipo = dto.Tipo.ToString(CultureInfo.InvariantCulture),
                SenhaUser = dto.SenhaUser?.Trim(),
                NomeImpCheque = dto.NomeImpCheque?.Trim(),
                NoMatric = dto.NoMatric?.Trim(),
                CdEmpresa = dto.CdEmpresa,
                CdFilial = dto.CdFilial,
                NoUser = dto.NoUser,
                Email = dto.Email?.Trim(),
                Ativo = dto.Ativo,
                NormalizedUserName = dto.NormalizedUserName?.Trim().ToUpperInvariant(),
                IdFuncionario = dto.IdFuncionario,
                FlNaoRecebeEmail = dto.FlNaoRecebeEmail
            };
            _db.Usuarios.Add(e);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(string codigo, UsuarioUpdateDto dto, CancellationToken ct = default)
        {
            var e = await _db.Usuarios.FirstOrDefaultAsync(u => u.Codigo == codigo, ct) ?? throw new KeyNotFoundException("Usuário não encontrado.");
            Validate(dto);
            e.Descricao = dto.Descricao.Trim();
            e.Tipo = dto.Tipo.ToString(CultureInfo.InvariantCulture);
            e.SenhaUser = dto.SenhaUser?.Trim();
            e.NomeImpCheque = dto.NomeImpCheque?.Trim();
            e.NoMatric = dto.NoMatric?.Trim();
            e.CdEmpresa = dto.CdEmpresa;
            e.CdFilial = dto.CdFilial;
            e.NoUser = dto.NoUser;
            e.Email = dto.Email?.Trim();
            e.Ativo = dto.Ativo;
            e.NormalizedUserName = dto.NormalizedUserName?.Trim().ToUpperInvariant();
            e.IdFuncionario = dto.IdFuncionario;
            e.FlNaoRecebeEmail = dto.FlNaoRecebeEmail;
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(string codigo, CancellationToken ct = default)
        {
            var e = await _db.Usuarios.FirstOrDefaultAsync(u => u.Codigo == codigo, ct) ?? throw new KeyNotFoundException("Usuário não encontrado.");
            _db.Usuarios.Remove(e);
            await _db.SaveChangesAsync(ct);
        }

        public async Task RedefinirSenhaPadraoAsync(IEnumerable<string> codigos, CancellationToken ct = default)
        {
            var set = codigos.Select(c => c.Trim().ToUpperInvariant()).ToHashSet();
            if (!await _db.Usuarios.AnyAsync(u => set.Contains(u.Codigo), ct)) throw new KeyNotFoundException("Nenhum usuário encontrado.");
            await Task.CompletedTask;
        }

        public async Task RedefinirSenhaPadraoUsuarioAsync(string codigo, CancellationToken ct = default)
        {
            if (!await _db.Usuarios.AnyAsync(u => u.Codigo == codigo, ct)) throw new KeyNotFoundException("Usuário não encontrado.");
            await Task.CompletedTask;
        }

        private static void Validate(UsuarioCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Codigo) || dto.Codigo.Length > 30) throw new ArgumentException("Login inválido (até 30 chars).");
            ValidateCommon(dto.Descricao, dto.Tipo, dto.SenhaUser, dto.NomeImpCheque, dto.NoMatric, dto.CdEmpresa, dto.CdFilial, dto.NoUser, dto.Email, dto.Ativo, dto.NormalizedUserName, dto.FlNaoRecebeEmail);
        }
        private static void Validate(UsuarioUpdateDto dto)
        {
            ValidateCommon(dto.Descricao, dto.Tipo, dto.SenhaUser, dto.NomeImpCheque, dto.NoMatric, dto.CdEmpresa, dto.CdFilial, dto.NoUser, dto.Email, dto.Ativo, dto.NormalizedUserName, dto.FlNaoRecebeEmail);
        }
        private static void ValidateCommon(string descricao, int tipo, string? senha, string? nomeImp, string? noMatric, int? cdEmp, int? cdFil, int noUser, string? email, string ativo, string? normalized, string? flNaoRecebeEmail)
        {
            if (string.IsNullOrWhiteSpace(descricao) || descricao.Length > 50) throw new ArgumentException("Nome inválido (até 50 chars).");
            if (tipo != 0 && tipo != 1) throw new ArgumentException("Tipo inválido (0=Prestador, 1=Empregado).");
            if (senha is { Length: > 20 }) throw new ArgumentException("Senha excede 20 caracteres.");
            if (nomeImp is { Length: > 50 }) throw new ArgumentException("Nome p/ cheque excede 50.");
            if (noMatric is { Length: > 8 }) throw new ArgumentException("Matrícula excede 8.");
            if (email is { Length: > 100 }) throw new ArgumentException("E-mail excede 100.");
            if (ativo is null || (ativo != "S" && ativo != "N")) throw new ArgumentException("Situação deve ser 'S' ou 'N'.");
            if (normalized is { Length: > 30 }) throw new ArgumentException("NormalizedUserName excede 30.");
            if (flNaoRecebeEmail is not null and not ("S" or "N")) throw new ArgumentException("flnaorecebeemail deve ser 'S' ou 'N' ou null.");
            if (noUser <= 0) throw new ArgumentException("NoUser deve ser > 0.");
        }
    }
}
