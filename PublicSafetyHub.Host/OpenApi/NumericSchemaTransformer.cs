using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace PublicSafetyHub.Host.OpenApi;

public class NumericSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken _)
    {
        var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        var numericType = Type.GetTypeCode(type) switch
        {
            TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 => JsonSchemaType.Integer,
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal => JsonSchemaType.Number,
            _ => (JsonSchemaType?)null
        };

        if (numericType.HasValue && !type.IsEnum)
        {
            schema.Type = numericType.Value | (schema.Type.GetValueOrDefault() & JsonSchemaType.Null);
            schema.AnyOf = null;
            schema.Pattern = null;
        }

        return Task.CompletedTask;
    }
}
