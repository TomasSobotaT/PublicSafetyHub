using PublicSafetyHub.Core.Models;
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

        if (openIddictRequest.ClientId is null || !authOptions.ClientIds.Contains(openIddictRequest.ClientId, StringComparer.Ordinal))
        {
            return TokenExchangeResult.Failure(Errors.InvalidClient, "Invalid client.");
        }

        if (openIddictRequest.IsPasswordGrantType())
        {
            if (string.IsNullOrWhiteSpace(openIddictRequest.Username) || openIddictRequest.Username.Length > 256 || string.IsNullOrEmpty(openIddictRequest.Password) || openIddictRequest.Password.Length > 128)
            {
                return TokenExchangeResult.Failure(Errors.InvalidGrant, "Invalid credentials or session.");
            }

            if (!openIddictRequest.HasScope(AuthConstants.ApiScope))
            {
                return TokenExchangeResult.Failure(Errors.InvalidScope, "The public-safety-hub-api scope is required.");
            }

            var login = await authService.LoginAsync(openIddictRequest.Username, openIddictRequest.Password, openIddictRequest.ClientId, openIddictRequest.GetScopes());

            return login.IsSuccess
                ? TokenExchangeResult.Success(login.Value)
                : TokenExchangeResult.Failure(Errors.InvalidGrant, login.Error.Message);
        }

        if (openIddictRequest.IsRefreshTokenGrantType())
        {
            var principal = await httpUserContext.GetRefreshTokenPrincipalAsync();

            if (principal is null)
            {
                return TokenExchangeResult.Failure(Errors.InvalidGrant, "Invalid credentials or session.");
            }

            var originalScopes = principal.GetScopes();
            var scopes = openIddictRequest.Scope is null ? originalScopes : openIddictRequest.GetScopes();

            if (scopes.Any(scope => !originalScopes.Contains(scope, StringComparer.Ordinal)))
            {
                return TokenExchangeResult.Failure(Errors.InvalidScope, "The granted scopes cannot be expanded.");
            }

            var refresh = await authService.RefreshAsync(principal, scopes);

            return refresh.IsSuccess
                ? TokenExchangeResult.Success(refresh.Value)
                : TokenExchangeResult.Failure(Errors.InvalidGrant, refresh.Error.Message);
        }

        return TokenExchangeResult.Failure(Errors.UnsupportedGrantType, "The grant type is not supported.");
    }

}
