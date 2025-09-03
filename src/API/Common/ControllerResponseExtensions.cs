using Microsoft.AspNetCore.Mvc;
using RhSensoWebApi.Core.Common.Exceptions;

namespace RhSensoWebApi.API.Common
{
    public static class ControllerResponseExtensions
    {
        public static IActionResult OkResponse<T>(this ControllerBase c, T data, string? message = null)
        {
            var resp = new BaseResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                TraceId = c.HttpContext.TraceIdentifier
            };
            return c.Ok(resp);
        }

        public static IActionResult CreatedResponse<T>(this ControllerBase c, string location, T data, string? message = null)
        {
            var resp = new BaseResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                TraceId = c.HttpContext.TraceIdentifier
            };
            return c.Created(location, resp);
        }

        public static IActionResult FailResponse(this ControllerBase c, int statusCode, string message, string? code = null, IDictionary<string, string[]>? errors = null)
        {
            var resp = new BaseResponse<object>
            {
                Success = false,
                Message = message,
                Error = new ErrorDto { Code = code ?? statusCode.ToString(), Message = message },
                Errors = errors,
                TraceId = c.HttpContext.TraceIdentifier
            };
            return new ObjectResult(resp) { StatusCode = statusCode };
        }
    }
}
