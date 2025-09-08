using RhSenso.Shared.Security;

namespace RhSenso.Shared.SEG.Botoes
{
    public sealed record BotaoKeyDto(string CodigoSistema, string CodigoFuncao, string Nome);

    /// <summary>
    /// DTO para listagem (grid). Agora expõe "Id" seguro (Base64Url da chave composta).
    /// </summary>
    public sealed record BotaoListDto(
        string CodigoSistema,
        string CodigoFuncao,
        string Nome,
        string Descricao,
        string Acao)
    {
        public string KeyRaw => $"{CodigoSistema}|{CodigoFuncao}|{Nome}";
        public string Id => KeyCodec.ToBase64Url(KeyRaw);
    }

    /// <summary>
    /// DTO para criar/editar botão.
    /// </summary>
    public sealed class BotaoFormDto
    {
        public string CodigoSistema { get; set; } = string.Empty;
        public string CodigoFuncao { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
    }
}
