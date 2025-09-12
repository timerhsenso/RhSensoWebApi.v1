// src/RhSensoWeb/TagHelpers/DisableWhenDeniedTagHelper.cs
#nullable enable
using Microsoft.AspNetCore.Razor.TagHelpers;
using RhSensoWeb.Services.Security;

namespace RhSensoWeb.TagHelpers;

[HtmlTargetElement(Attributes = "app-disable-when-denied, app-sistema, app-funcao")]
public sealed class DisableWhenDeniedTagHelper : TagHelper
{
    private readonly IPermissionProvider _permissions;
    public DisableWhenDeniedTagHelper(IPermissionProvider permissions) => _permissions = permissions;

    [HtmlAttributeName("app-disable-when-denied")] public bool Enabled { get; set; } = true;
    [HtmlAttributeName("app-sistema")] public string Sistema { get; set; } = string.Empty;
    [HtmlAttributeName("app-funcao")] public string Funcao { get; set; } = string.Empty;
    [HtmlAttributeName("app-acao")] public string? Acoes { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (!Enabled) return;

        var sistema = (Sistema ?? "").Trim();
        var funcao = (Funcao ?? "").Trim();
        if (string.IsNullOrWhiteSpace(sistema) || string.IsNullOrWhiteSpace(funcao)) return;

        bool allowed;
        if (string.IsNullOrWhiteSpace(Acoes))
        {
            allowed = await _permissions.HasFeatureAsync(sistema, funcao);
        }
        else
        {
            var tokens = Acoes.Replace(",", " ")
                              .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                              .SelectMany(t => t.Trim().ToUpperInvariant().ToCharArray())
                              .Distinct();
            allowed = false;
            foreach (var ch in tokens)
            {
                if (await _permissions.HasActionAsync(sistema, funcao, ch)) { allowed = true; break; }
            }
        }

        if (!allowed)
        {
            // atributo disabled + title pra UX
            output.Attributes.SetAttribute("disabled", "disabled");
            var title = output.Attributes.FirstOrDefault(a => a.Name == "title")?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(title))
                output.Attributes.SetAttribute("title", "Você não tem permissão para esta ação");
        }
    }
}
