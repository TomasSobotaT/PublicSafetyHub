using Asp.Versioning;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authorization;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class OpenApiServiceCollectionExtension
{
    public static IServiceCollection AddVersionedOpenApi(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddMvc().AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        }).AddOpenApi(versionedOptions =>
        {
            var options = versionedOptions.Document;
            options.AddOperationTransformer<TokenOperationTransformer>();
            options.AddDocumentTransformer((document, context, _) =>
            {
                document.Info.Title = "PublicSafetyHub API";
                document.Info.Version = context.DocumentName;
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Paste the access_token returned by the token endpoint."
                };

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;

                if (metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any())
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
                    });
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
