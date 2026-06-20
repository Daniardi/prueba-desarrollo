using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CourierMax.Api.Api;

public sealed class SpanishEnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum)
        {
            return;
        }

        schema.Type = "string";
        schema.Format = null;
        schema.Enum = SpanishEnumNames.GetNames(context.Type)
            .Select(x => (IOpenApiAny)new OpenApiString(x))
            .ToList();
    }
}
