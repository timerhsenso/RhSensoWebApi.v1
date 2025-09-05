
namespace RhSenso.Shared.SEG.Botoes
{
    public sealed record BotaoKeyDto(string CodigoSistema, string CodigoFuncao, string Nome);
    public sealed record BotaoListDto(string CodigoSistema, string CodigoFuncao, string Nome, string Descricao, string Acao);
    public sealed class BotaoFormDto
    {
        public string CodigoSistema { get; set; } = string.Empty;
        public string CodigoFuncao { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
    }
}
