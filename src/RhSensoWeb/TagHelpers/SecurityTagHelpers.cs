// src/RhSensoWeb/TagHelpers/SecurityTagHelpers.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using RhSensoWeb.Services.Security;

namespace RhSensoWeb.TagHelpers
{
    /// <summary>
    /// TagHelper que ESCONDE elementos se não tiver permissão
    /// Uso: <button app-when-allowed app-sistema="RHU" app-funcao="RHU_FM_PPRA_ESOCIAL" app-acao="C">Criar</button>
    /// </summary>
    [HtmlTargetElement("*", Attributes = "app-when-allowed,app-sistema,app-funcao")]
    public sealed class AppWhenAllowedTagHelper : TagHelper
    {
        private readonly IPermissionProvider _perms;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<AppWhenAllowedTagHelper> _logger;

        public AppWhenAllowedTagHelper(
            IPermissionProvider perms,
            IHttpContextAccessor http,
            ILogger<AppWhenAllowedTagHelper> logger)
        {
            _perms = perms;
            _http = http;
            _logger = logger;
        }

        [HtmlAttributeName("app-when-allowed")]
        public bool WhenAllowed { get; set; } = true;

        [HtmlAttributeName("app-sistema")]
        public string Sistema { get; set; } = "";

        [HtmlAttributeName("app-funcao")]
        public string Funcao { get; set; } = "";

        [HtmlAttributeName("app-acao")]
        public string? Acao { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            _logger.LogInformation("🏷️ TagHelper WHEN-ALLOWED: Sistema={Sistema}, Funcao={Funcao}, Acao={Acao}",
                Sistema, Funcao, Acao ?? "null");

            if (!WhenAllowed)
            {
                _logger.LogInformation("❌ WhenAllowed=false, suprimindo elemento");
                output.SuppressOutput();
                return;
            }

            // Normaliza parâmetros
            var sistema = (Sistema ?? "").Trim();
            var funcao = (Funcao ?? "").Trim();

            if (string.IsNullOrWhiteSpace(sistema) || string.IsNullOrWhiteSpace(funcao))
            {
                _logger.LogWarning("❌ Sistema ou Função vazios, suprimindo elemento");
                output.SuppressOutput();
                return;
            }

            try
            {
                // Garante que as permissões estejam carregadas
                await _perms.EnsureLoadedAsync(_http.HttpContext!);

                bool allowed;

                if (string.IsNullOrWhiteSpace(Acao))
                {
                    // Só verifica se tem a função
                    allowed = await _perms.HasFeatureAsync(sistema, funcao);
                    _logger.LogInformation("✅ Verificou FUNÇÃO: {Resultado}", allowed);
                }
                else
                {
                    // Verifica ação específica
                    var firstChar = Acao!.Trim().ToUpperInvariant()[0];
                    allowed = await _perms.HasActionAsync(sistema, funcao, firstChar);
                    _logger.LogInformation("✅ Verificou AÇÃO '{Char}': {Resultado}", firstChar, allowed);
                }

                if (!allowed)
                {
                    _logger.LogInformation("❌ Permissão NEGADA - elemento será SUPRIMIDO");
                    output.SuppressOutput();
                }
                else
                {
                    _logger.LogInformation("✅ Permissão OK - elemento será EXIBIDO");
                    // Não faz nada, deixa o elemento aparecer
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERRO no TagHelper - suprimindo por segurança");
                output.SuppressOutput();
            }
        }
    }

    /// <summary>
    /// TagHelper que DESABILITA elementos se não tiver permissão (mas mantém visível)
    /// Uso: <button app-disable-when-denied app-sistema="RHU" app-funcao="RHU_FM_PPRA_ESOCIAL" app-acao="I">Importar</button>
    /// </summary>
    [HtmlTargetElement("*", Attributes = "app-disable-when-denied,app-sistema,app-funcao")]
    public sealed class AppDisableWhenDeniedTagHelper : TagHelper
    {
        private readonly IPermissionProvider _perms;
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<AppDisableWhenDeniedTagHelper> _logger;

        public AppDisableWhenDeniedTagHelper(
            IPermissionProvider perms,
            IHttpContextAccessor http,
            ILogger<AppDisableWhenDeniedTagHelper> logger)
        {
            _perms = perms;
            _http = http;
            _logger = logger;
        }

        [HtmlAttributeName("app-disable-when-denied")]
        public bool DisableWhenDenied { get; set; } = true;

        [HtmlAttributeName("app-sistema")]
        public string Sistema { get; set; } = "";

        [HtmlAttributeName("app-funcao")]
        public string Funcao { get; set; } = "";

        [HtmlAttributeName("app-acao")]
        public string? Acao { get; set; }

        [HtmlAttributeName("app-disabled-title")]
        public string? DisabledTitle { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (!DisableWhenDenied) return;

            var sistema = (Sistema ?? "").Trim();
            var funcao = (Funcao ?? "").Trim();

            if (string.IsNullOrWhiteSpace(sistema) || string.IsNullOrWhiteSpace(funcao))
                return;

            try
            {
                await _perms.EnsureLoadedAsync(_http.HttpContext!);

                bool allowed;

                if (string.IsNullOrWhiteSpace(Acao))
                {
                    allowed = await _perms.HasFeatureAsync(sistema, funcao);
                }
                else
                {
                    var firstChar = Acao!.Trim().ToUpperInvariant()[0];
                    allowed = await _perms.HasActionAsync(sistema, funcao, firstChar);
                }

                if (!allowed)
                {
                    output.Attributes.SetAttribute("disabled", "disabled");
                    var title = DisabledTitle ?? "Você não tem permissão para esta ação";
                    output.Attributes.SetAttribute("title", title);

                    _logger.LogInformation("⚠️ Elemento DESABILITADO por falta de permissão");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro no TagHelper DISABLE - desabilitando por segurança");
                output.Attributes.SetAttribute("disabled", "disabled");
                output.Attributes.SetAttribute("title", "Erro ao verificar permissões");
            }
        }
    }
}