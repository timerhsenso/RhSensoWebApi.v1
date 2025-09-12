// src/RhSensoWeb/Services/Security/RequirePermissionAttribute.cs
#nullable enable
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RhSensoWeb.Services.Security;

/// <summary>
/// Usa IPermissionProvider para checar sistema/função/ação antes de executar a action.
/// Ex.: [RequirePermission("RHU", "RHU_FM_PFERIAS", "C")]
/// </summary>
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string sistema, string funcao, string? acoes = null)
        : base(typeof(RequirePermissionFilter))
    {
        Arguments = new object[] { sistema, funcao, acoes };
    }
}

internal sealed class RequirePermissionFilter : IAsyncActionFilter
{
    private readonly IPermissionProvider _perms;
    private readonly string _sistema;
    private readonly string _funcao;
    private readonly string? _acoes;

    public RequirePermissionFilter(IPermissionProvider perms, string sistema, string funcao, string? acoes)
    {
        _perms = perms;
        _sistema = sistema;
        _funcao = funcao;
        _acoes = acoes;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // não logado? manda pro login
        if (!(context.HttpContext.User?.Identity?.IsAuthenticated ?? false))
        {
            context.Result = new ChallengeResult(); // respeita o cookie scheme (vai ao /Account/Login)
            return;
        }

        bool allowed;

        if (string.IsNullOrWhiteSpace(_acoes))
        {
            // só precisa possuir a função
            allowed = await _perms.HasFeatureAsync(_sistema, _funcao);
        }
        else
        {
            // basta ter UMA letra das ações informadas (aceita "C,E" ou "CE")
            var tokens = _acoes
                .Replace(",", " ", StringComparison.Ordinal)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(t => t.Trim().ToUpperInvariant().ToCharArray())
                .Distinct();

            allowed = false;
            foreach (var ch in tokens)
            {
                if (await _perms.HasActionAsync(_sistema, _funcao, ch))
                {
                    allowed = true;
                    break;
                }
            }
        }

        if (!allowed)
        {
            // opcional: redireciona para uma tela de acesso negado da UI
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
            return;
        }

        await next();
    }
}
