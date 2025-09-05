// src/RhSenso.Shared/SEG/Botoes/BotoesDtos.cs
using System.ComponentModel.DataAnnotations;

namespace RhSenso.Shared.SEG.Botoes
{
    /// <summary>Chave composta do botão (Sistema/Função/Nome).</summary>
    public sealed record BotaoKeyDto(
        string CodigoSistema,
        string CodigoFuncao,
        string Nome
    );

    /// <summary>DTO enxuto para listagem.</summary>
    public sealed record BotaoListDto(
        string CodigoSistema,
        string CodigoFuncao,
        string Nome,
        string Descricao,
        string Acao
    );

    /// <summary>DTO para criação/edição (validado na App e na API).</summary>
    public sealed class BotaoFormDto
    {
        [Required, StringLength(10, MinimumLength = 1)]
        public string CodigoSistema { get; set; } = string.Empty;

        [Required, StringLength(30, MinimumLength = 1)]
        public string CodigoFuncao { get; set; } = string.Empty;

        [Required, StringLength(30, MinimumLength = 1)]
        public string Nome { get; set; } = string.Empty;

        [Required, StringLength(60, MinimumLength = 1)]
        public string Descricao { get; set; } = string.Empty;

        // cdacao é char(1) → force 1 caractere
        [Required, StringLength(1, MinimumLength = 1)]
        public string Acao { get; set; } = "I"; // valor default opcional
    }
}
