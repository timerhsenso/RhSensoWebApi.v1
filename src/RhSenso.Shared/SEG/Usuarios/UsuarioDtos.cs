using System;

namespace RhSenso.Shared.SEG.Usuarios
{
    public sealed record UsuarioListDto(string Codigo, string Descricao, string TipoStr, string? Email, string Situacao, int NoUser, int? CdEmpresa, int? CdFilial);
    public sealed record UsuarioCreateDto(string Codigo, string Descricao, int Tipo, string? SenhaUser, string? NomeImpCheque, string? NoMatric, int? CdEmpresa, int? CdFilial, int NoUser, string? Email, string Ativo, string? NormalizedUserName, Guid? IdFuncionario, string? FlNaoRecebeEmail);
    public sealed record UsuarioUpdateDto(string Descricao, int Tipo, string? SenhaUser, string? NomeImpCheque, string? NoMatric, int? CdEmpresa, int? CdFilial, int NoUser, string? Email, string Ativo, string? NormalizedUserName, Guid? IdFuncionario, string? FlNaoRecebeEmail);
}
