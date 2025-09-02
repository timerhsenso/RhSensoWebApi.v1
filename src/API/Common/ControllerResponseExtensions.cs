using Microsoft.AspNetCore.Mvc;
using RhSensoWebApi.Core.Common.Exceptions; // BaseResponse<T>, ErrorDto

namespace RhSensoWebApi.API.Common
{
    /// <summary>
    /// Helpers padronizados de resposta, alinhados ao BaseResponse<T> atual (Success, Data, Error, Timestamp).
    /// </summary>
    public static class ControllerResponseExtensions
    {
        public static IActionResult OkResponse<T>(this ControllerBase c, T data, string? message = null)
        {
            var resp = new BaseResponse<T>
            {
                Success = true,
                Data = data
                // Error = null, Timestamp já é definido no ctor
            };
            // Se quiser incluir uma mensagem de sucesso, hoje o seu BaseResponse não tem campo dedicado.
            // Alternativas: criar um DTO para o Data que carregue mensagem, ou evoluir o BaseResponse no Core.
            return c.Ok(resp);
        }

        public static IActionResult CreatedResponse<T>(this ControllerBase c, string location, T data, string? message = null)
        {
            var resp = new BaseResponse<T>
            {
                Success = true,
                Data = data
            };
            return c.Created(location, resp);
        }

        public static IActionResult FailResponse(this ControllerBase c, int statusCode, string message, string? code = null)
        {
            var resp = new BaseResponse<object>
            {
                Success = false,
                Error = new ErrorDto
                {
                    Code = code ?? statusCode.ToString(),
                    Message = message
                }
            };
            return new ObjectResult(resp) { StatusCode = statusCode };
        }
    }
}
