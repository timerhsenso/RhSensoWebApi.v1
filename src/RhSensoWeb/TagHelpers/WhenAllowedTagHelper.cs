// src/RhSensoWeb/TagHelpers/WhenAllowedTagHelper.cs
#nullable enable
using Microsoft.AspNetCore.Razor.TagHelpers;
using RhSensoWeb.Services.Security;

namespace RhSensoWeb.TagHelpers;

/// <summary>
/// Exemplo de uso em qualquer .cshtml:
/// <button class="btn btn-primary"
///         app-when-allowed
///         app-sistema="RHU"
///         app-funcao="RHU_FM_PFERIAS"
///         app-acao="C">
///   Criar
/// </button>
/// - Se o usuário tiver a função, o elemento aparece;
/// - Se informar app-acao, basta conter UMA das letras (C,E,I,...) para aparecer.
/// </summary>
[HtmlTargetElement(Attributes = "app-when-allowed, app-sistema, app-funcao")]
public sealed class WhenAllowedTagHelper : TagHelper
{
    private readonly IPermissionProvider _permissions;

    public WhenAllowedTagHelper(IPermissionProvider permissions)
        => _permissions = permissions;

    /// <summary>Marcador sem valor, apenas para ativar o helper.</summary>
    [HtmlAttributeName("app-when-allowed")]
    public bool Enabled { get; set; } = true;

    /// <summary>Código do sistema (ex.: "RHU").</summary>
    [HtmlAttributeName("app-sistema")]
    public string Sistema { get; set; } = string.Empty;

    /// <summary>Código da função/tela (ex.: "RHU_FM_PFERIAS").</summary>
    [HtmlAttributeName("app-funcao")]
    public string Funcao { get; set; } = string.Empty;

    /// <summary>
    /// Letras de ação (ex.: "C", "E", "CI", "C,E").  
    /// Se vazio, valida apenas a existência da função.
    /// </summary>
    [HtmlAttributeName("app-acao")]
    public string? Acoes { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (!Enabled)
        {
            output.SuppressOutput();
            return;
        }

        // normaliza
        var sistema = (Sistema ?? string.Empty).Trim();
        var funcao = (Funcao ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(sistema) || string.IsNullOrWhiteSpace(funcao))
        {
            output.SuppressOutput();
            return;
        }

        bool allowed;

        // Sem ação => basta ter a função
        if (string.IsNullOrWhiteSpace(Acoes))
        {
            allowed = await _permissions.HasFeatureAsync(sistema, funcao);
        }
        else
        {
            // Divide por vírgula/espaço; aceita "CI", "C,E", "C E"
            var tokens = Acoes!
                .Replace(",", " ", StringComparison.Ordinal)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(t => t.Trim().ToUpperInvariant().ToCharArray())
                .Distinct();

            allowed = false;
            foreach (var a in tokens)
            {
                if (await _permissions.HasActionAsync(sistema, funcao, a))
                {
                    allowed = true; // basta UMA ação bater
                    break;
                }
            }
        }

        if (!allowed)
            output.SuppressOutput(); // some com o elemento do HTML
    }
}
