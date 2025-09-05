using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

namespace RhSensoWebApi.API.Swagger
{
    public sealed class ErrorDtoSchemaExample : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.Name == "ErrorDto")
            {
                var obj = new
                {
                    code = "BadRequest",
                    message = "Falha de validação.",
                    traceId = "00-9f7c8d56f5b2a14e3e2d1c3b5f1d2a3b-abc123def456ghi7-00",
                    errors = new { email = new[] { "Campo obrigatório." }, senha = new[] { "Tamanho mínimo: 6." } }
                };

                schema.Example = OpenApiAnyFactory.CreateFromJson(JsonSerializer.Serialize(obj));
            }
        }
    }

    public sealed class BaseResponseSchemaExample : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsGenericType && context.Type.Name.StartsWith("BaseResponse"))
            {
                var ok = new
                {
                    success = true,
                    message = "Operação realizada com sucesso.",
                    data = new { id = 123, nome = "Exemplo" },
                    timestamp = "2025-09-03T18:00:00Z"
                };

                schema.Example = OpenApiAnyFactory.CreateFromJson(JsonSerializer.Serialize(ok));
            }
        }
    }
}
