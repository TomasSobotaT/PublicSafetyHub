using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace PublicSafetyHub.Host.OpenApi;

public class OperationIdTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken _)
    {
        var routeValues = context.Description.ActionDescriptor.RouteValues;
        operation.OperationId = $"{routeValues["controller"]}_{routeValues["action"]}";

        return Task.CompletedTask;
    }
}
