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

        operation.Summary = "Exchange an authorization code or refresh tokens";
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
                            ["grant_type"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "Use authorization_code or refresh_token." },
                            ["client_id"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = $"An enabled public client ID, for example {authOptions.Clients[0].ClientId}. No client_secret." },
                            ["code"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "The single-use code returned by /connect/authorize." },
                            ["redirect_uri"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri", Description = "For authorization_code: the exact redirect URI used in the authorization request." },
                            ["code_verifier"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "password", Description = "For authorization_code: the original PKCE verifier. Only S256 challenges are supported." },
                            ["refresh_token"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "password", Description = "Required for refresh_token. Send only to the token endpoint." },
                            ["scope"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = $"Authorize with openid roles offline_access {AuthConstants.ApiScope}. On refresh, omit scope to retain granted scopes, or provide a subset." }
                        }
                    }
                }
            }
        };

        return Task.CompletedTask;
    }
}
