using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Auth;
using PublicSafetyHub.Base.UserContext;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace PublicSafetyHub.Application.Services;

public class TokenService(IAuthService authService, IAuthOptions authOptions, IHttpUserContext httpUserContext) : ITokenService
{
    public async Task<IActionResult> ExchangeAsync()
    {
        var openIddictRequest = httpUserContext.GetTokenRequest();

        if (openIddictRequest.ClientId is null || !authOptions.ClientIds.Contains(openIddictRequest.ClientId, StringComparer.Ordinal))
        {
            return OAuthError(Errors.InvalidClient, "Invalid client.");
        }

        if (openIddictRequest.IsPasswordGrantType())
        {
            if (string.IsNullOrWhiteSpace(openIddictRequest.Username) || openIddictRequest.Username.Length > 256 || string.IsNullOrEmpty(openIddictRequest.Password) || openIddictRequest.Password.Length > 128)
            {
                return OAuthError(Errors.InvalidGrant, "Invalid credentials or session.");
            }

            if (!openIddictRequest.HasScope(AuthConstants.ApiScope))
            {
                return OAuthError(Errors.InvalidScope, "The public-safety-hub-api scope is required.");
            }

            var login = await authService.LoginAsync(openIddictRequest.Username, openIddictRequest.Password, openIddictRequest.ClientId, openIddictRequest.GetScopes());

            return login.IsSuccess
                ? new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, login.Value)
                : OAuthError(Errors.InvalidGrant, login.Error.Message);
        }

        if (openIddictRequest.IsRefreshTokenGrantType())
        {
            var principal = await httpUserContext.GetRefreshTokenPrincipalAsync();

            if (principal is null)
            {
                return OAuthError(Errors.InvalidGrant, "Invalid credentials or session.");
            }

            var originalScopes = principal.GetScopes();
            var scopes = openIddictRequest.Scope is null ? originalScopes : openIddictRequest.GetScopes();

            if (scopes.Any(scope => !originalScopes.Contains(scope, StringComparer.Ordinal)))
            {
                return OAuthError(Errors.InvalidScope, "The granted scopes cannot be expanded.");
            }

            var refresh = await authService.RefreshAsync(principal, scopes);

            return refresh.IsSuccess
                ? new SignInResult(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, refresh.Value)
                : OAuthError(Errors.InvalidGrant, refresh.Error.Message);
        }

        return OAuthError(Errors.UnsupportedGrantType, "The grant type is not supported.");
    }

    private static ForbidResult OAuthError(string error, string description) {

        return new ForbidResult(
           OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
           new AuthenticationProperties(new Dictionary<string, string>
           {
               [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
               [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
           }));
    }
}
