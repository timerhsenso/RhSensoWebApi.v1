// -----------------------------------------------------------------------------
// RhSensoWeb - TagHelpers de segurança
// - app-when-allowed        → esconde o elemento se o usuário NÃO tiver permissão
// - app-disable-when-denied → mantém visível, mas desabilita se NÃO tiver permissão
// Ambos garantem que as permissões estejam carregadas.
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using RhSensoWeb.Services.Security;

namespace RhSensoWeb.TagHelpers
{
    // -------------------------------------------------------------------------
    // <button app-when-allowed app-sistema="RHU" app-funcao="RHU_FM_PFERIAS" app-acao="C">...</button>
    // Se 'acao' não for informada, apenas checa se a função existe para o sistema.
    // -------------------------------------------------------------------------
    [HtmlTargetElement("*", Attributes = "app-when-allowed,app-sistema,app-funcao")]
    public sealed class AppWhenAllowedTagHelper : TagHelper
    {
        private readonly IPermissionProvider _perms;
        private readonly IHttpContextAccessor _http;

        public AppWhenAllowedTagHelper(IPermissionProvider perms, IHttpContextAccessor http)
        {
            _perms = perms;
            _http = http;
        }

        [HtmlAttributeName("app-sistema")]
        public string Sistema { get; set; } = "";

        [HtmlAttributeName("app-funcao")]
        public string Funcao { get; set; } = "";

        // Opcional: se informado, checa ação específica
        [HtmlAttributeName("app-acao")]
        public string? Acao { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            await _perms.EnsureLoadedAsync(_http.HttpContext!); // garante cache

            bool allowed;
            if (!string.IsNullOrWhiteSpace(Acao))
            {
                var c = Acao!.Trim()[0];
                allowed = await _perms.HasActionAsync(Sistema, Funcao, c);
            }
            else
            {
                allowed = await _perms.HasFeatureAsync(Sistema, Funcao);
            }

            if (!allowed)
                output.SuppressOutput(); // some do HTML
        }
    }

    // -------------------------------------------------------------------------
    // <button app-disable-when-denied app-sistema="RHU" app-funcao="RHU_FM_PFERIAS" app-acao="I" app-disabled-title="Sem permissão">
    // Mantém o elemento, mas aplica disabled quando negar.
    // -------------------------------------------------------------------------
    [HtmlTargetElement("*", Attributes = "app-disable-when-denied,app-sistema,app-funcao")]
    public sealed class AppDisableWhenDeniedTagHelper : TagHelper
    {
        private readonly IPermissionProvider _perms;
        private readonly IHttpContextAccessor _http;

        public AppDisableWhenDeniedTagHelper(IPermissionProvider perms, IHttpContextAccessor http)
        {
            _perms = perms;
            _http = http;
        }

        // basta a presença do atributo
        [HtmlAttributeName("app-disable-when-denied")]
        public bool DisableWhenDenied { get; set; } = true;

        [HtmlAttributeName("app-sistema")]
        public string Sistema { get; set; } = "";

        [HtmlAttributeName("app-funcao")]
        public string Funcao { get; set; } = "";

        [HtmlAttributeName("app-acao")]
        public string? Acao { get; set; }

        // opcional: título quando desabilitado
        [HtmlAttributeName("app-disabled-title")]
        public string? DisabledTitle { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (!DisableWhenDenied) return;

            await _perms.EnsureLoadedAsync(_http.HttpContext!); // garante cache

            bool allowed;
            if (!string.IsNullOrWhiteSpace(Acao))
            {
                var c = Acao!.Trim()[0];
                allowed = await _perms.HasActionAsync(Sistema, Funcao, c);
            }
            else
            {
                allowed = await _perms.HasFeatureAsync(Sistema, Funcao);
            }

            if (!allowed)
            {
                output.Attributes.SetAttribute("disabled", "disabled"); // desabilita
                if (!string.IsNullOrWhiteSpace(DisabledTitle))
                    output.Attributes.SetAttribute("title", DisabledTitle);
            }
        }
    }
}
