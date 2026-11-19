using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authorization;
using PublicSafetyHub.Host.OpenApi;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class OpenApiServiceCollectionExtension
{
    extension(WebApplication app)
    {
        public WebApplication UseOpenApi()
        {
            app.MapOpenApi().WithDocumentPerVersion();
            app.UseSwaggerUI(options =>
            {
                var versions = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
                foreach (var version in versions.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/openapi/{version.GroupName}.json", $"PublicSafetyHub API {version.GroupName}");
                }
            });

            return app;
        }
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddOpenApiRegistration()
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
                options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
                options.AddSchemaTransformer<NumericSchemaTransformer>();
                options.AddOperationTransformer<OperationIdTransformer>();
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
}
