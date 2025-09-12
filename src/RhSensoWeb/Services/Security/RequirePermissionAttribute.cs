using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace RhSensoWeb.Services.Security;

/// <summary>
/// Filtro de autorização para controle de permissões por sistema, função e botão
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _sistema;
    private readonly string _funcao;
    private readonly string _botao;

    /// <summary>
    /// Construtor para verificação de permissão específica
    /// </summary>
    /// <param name="sistema">Código do sistema (ex: SEG, CAD, FIN)</param>
    /// <param name="funcao">Código da função (ex: USUARIOS, FORNECEDORES)</param>
    /// <param name="botao">Código do botão (ex: INCLUIR, ALTERAR, EXCLUIR)</param>
    public RequirePermissionAttribute(string sistema, string funcao, string botao)
    {
        _sistema = sistema ?? throw new ArgumentNullException(nameof(sistema));
        _funcao = funcao ?? throw new ArgumentNullException(nameof(funcao));
        _botao = botao ?? throw new ArgumentNullException(nameof(botao));
    }

    /// <summary>
    /// Construtor para verificação de permissão de função (sem botão específico)
    /// </summary>
    /// <param name="sistema">Código do sistema</param>
    /// <param name="funcao">Código da função</param>
    public RequirePermissionAttribute(string sistema, string funcao)
    {
        _sistema = sistema ?? throw new ArgumentNullException(nameof(sistema));
        _funcao = funcao ?? throw new ArgumentNullException(nameof(funcao));
        _botao = string.Empty;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Verifica se o usuário está autenticado
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
            return;
        }

        // Verifica se o usuário tem a permissão necessária
        if (!HasPermission(context.HttpContext.User))
        {
            // Para requisições AJAX, retorna 403 Forbidden
            if (IsAjaxRequest(context.HttpContext.Request))
            {
                context.Result = new JsonResult(new
                {
                    success = false,
                    message = "Acesso negado. Você não tem permissão para executar esta ação.",
                    permissionRequired = $"{_sistema}:{_funcao}:{_botao}"
                })
                {
                    StatusCode = 403
                };
                return;
            }

            // Para requisições normais, redireciona para página de acesso negado
            context.Result = new RedirectToActionResult("AccessDenied", "Error", new { area = "" });
        }
    }

    /// <summary>
    /// Verifica se o usuário tem a permissão necessária
    /// </summary>
    private bool HasPermission(ClaimsPrincipal user)
    {
        try
        {
            // Verifica se é super usuário (bypass de permissões)
            var isSuperUser = user.HasClaim("SuperUser", "true");
            if (isSuperUser)
                return true;

            // Constrói a chave da permissão
            var permissionKey = string.IsNullOrEmpty(_botao)
                ? $"{_sistema}:{_funcao}"
                : $"{_sistema}:{_funcao}:{_botao}";

            // Verifica se o usuário tem a permissão específica
            var hasPermission = user.HasClaim("Permission", permissionKey);

            // Log para debug (opcional)
            if (!hasPermission)
            {
                var userPermissions = user.Claims
                    .Where(c => c.Type == "Permission")
                    .Select(c => c.Value)
                    .ToList();

                // Aqui você pode adicionar logging se necessário
                // _logger?.LogWarning("User {UserId} denied access to {Permission}. User permissions: {UserPermissions}", 
                //     user.FindFirst(ClaimTypes.NameIdentifier)?.Value, permissionKey, string.Join(", ", userPermissions));
            }

            return hasPermission;
        }
        catch (Exception)
        {
            // Em caso de erro na verificação, nega o acesso por segurança
            return false;
        }
    }

    /// <summary>
    /// Verifica se a requisição é AJAX
    /// </summary>
    private static bool IsAjaxRequest(HttpRequest request)
    {
        return request.Headers.ContainsKey("X-Requested-With") &&
               request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }

    /// <summary>
    /// Propriedades para facilitar o acesso aos valores (somente leitura)
    /// </summary>
    public string Sistema => _sistema;
    public string Funcao => _funcao;
    public string Botao => _botao;
}

/// <summary>
/// Extensões para facilitar o uso do filtro de permissões
/// </summary>
public static class PermissionExtensions
{
    /// <summary>
    /// Verifica se o usuário tem uma permissão específica
    /// </summary>
    public static bool HasPermission(this ClaimsPrincipal user, string sistema, string funcao, string botao = "")
    {
        if (!user.Identity?.IsAuthenticated ?? true)
            return false;

        // Super usuário tem acesso a tudo
        if (user.HasClaim("SuperUser", "true"))
            return true;

        var permissionKey = string.IsNullOrEmpty(botao)
            ? $"{sistema}:{funcao}"
            : $"{sistema}:{funcao}:{botao}";

        return user.HasClaim("Permission", permissionKey);
    }

    /// <summary>
    /// Obtém todas as permissões do usuário
    /// </summary>
    public static IEnumerable<string> GetPermissions(this ClaimsPrincipal user)
    {
        return user.Claims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value);
    }

    /// <summary>
    /// Verifica se o usuário é super usuário
    /// </summary>
    public static bool IsSuperUser(this ClaimsPrincipal user)
    {
        return user.HasClaim("SuperUser", "true");
    }
}