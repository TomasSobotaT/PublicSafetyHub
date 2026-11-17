using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using PublicSafetyHub.Base.Auth;

namespace PublicSafetyHub.Host.OpenApi;

public class TokenOperationTransformer(IAuthOptions authOptions) : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken _)
    {
        if (context.Description.HttpMethod != "POST" || context.Description.RelativePath?.EndsWith("/auth/token", StringComparison.OrdinalIgnoreCase) != true)
        {
            return Task.CompletedTask;
        }

        operation.Summary = "Sign in or refresh tokens";
        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/x-www-form-urlencoded"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.Object,
                        Required = new HashSet<string> { "grant_type", "client_id" },
                        Properties = new Dictionary<string, IOpenApiSchema>
                        {
                            ["grant_type"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "Use password to sign in or refresh_token to renew tokens." },
                            ["client_id"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = $"A configured public client ID, for example {authOptions.ClientIds[0]}." },
                            ["username"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "email", Description = "Account email. Required for the password grant." },
                            ["password"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "password", Description = "Account password. Required for the password grant." },
                            ["refresh_token"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "password", Description = "Required for the refresh_token grant. Omit username and password." },
                            ["scope"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = $"Request offline_access {AuthConstants.ApiScope} when signing in. On refresh, omit to keep the granted scopes or provide a subset." }
                        }
                    }
                }
            }
        };

        return Task.CompletedTask;
    }
}
