using OpenIddict.Abstractions;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Auth;
using PublicSafetyHub.Base.UserContext;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace PublicSafetyHub.Application.Services;

public class TokenService(IAuthService authService, IAuthOptions authOptions, IHttpUserContext httpUserContext) : ITokenService
{
    public async Task<TokenExchangeResult> ExchangeAsync()
    {
        var openIddictRequest = httpUserContext.GetTokenRequest();

        if (openIddictRequest.ClientId is null || !authOptions.Clients.Any(client => client.Enabled && string.Equals(client.ClientId, openIddictRequest.ClientId, StringComparison.Ordinal)))
        {
            return TokenExchangeResult.Failure(Errors.InvalidClient, "Invalid client.");
        }

        if (openIddictRequest.IsAuthorizationCodeGrantType() || openIddictRequest.IsRefreshTokenGrantType())
        {
            var principal = await httpUserContext.GetTokenPrincipalAsync();

            if (principal is null)
            {
                return TokenExchangeResult.Failure(Errors.InvalidGrant, "Invalid credentials or session.");
            }

            var originalScopes = principal.GetScopes();
            var scopes = openIddictRequest.IsRefreshTokenGrantType() && openIddictRequest.Scope is not null
                ? openIddictRequest.GetScopes() : originalScopes;

            if (scopes.Any(scope => !originalScopes.Contains(scope, StringComparer.Ordinal)))
            {
                return TokenExchangeResult.Failure(Errors.InvalidScope, "The granted scopes cannot be expanded.");
            }

            var client = authOptions.Clients.Single(value => string.Equals(value.ClientId, openIddictRequest.ClientId, StringComparison.Ordinal));
            if (scopes.Any(scope => !client.Scopes.Contains(scope, StringComparer.Ordinal)))
            {
                return TokenExchangeResult.Failure(Errors.InvalidScope, "The granted scopes are no longer allowed for this client.");
            }

            var refresh = await authService.RefreshAsync(principal, scopes);

            return refresh.IsSuccess
                ? TokenExchangeResult.Success(refresh.Value)
                : TokenExchangeResult.Failure(Errors.InvalidGrant, refresh.Error.Message);
        }

        return TokenExchangeResult.Failure(Errors.UnsupportedGrantType, "The grant type is not supported.");
    }

}
