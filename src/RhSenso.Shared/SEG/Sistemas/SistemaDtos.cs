namespace RhSenso.Shared.SEG.Sistemas
{
    public sealed record SistemaListDto(string Codigo, string Descricao);
    public sealed record SistemaCreateDto(string Codigo, string Descricao);
    public sealed record SistemaUpdateDto(string Descricao);
}
