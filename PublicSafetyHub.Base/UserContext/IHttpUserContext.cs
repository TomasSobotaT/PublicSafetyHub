using System.Security.Claims;
using OpenIddict.Abstractions;
using PublicSafetyHub.Base.Auth;

namespace PublicSafetyHub.Base.UserContext;

public interface IHttpUserContext
{
    int? GetUserId();

    string GetSessionId();

    OpenIddictRequest GetTokenRequest();

    AuthorizationLoginSubmission GetAuthorizationSubmission();

    Task<ClaimsPrincipal> GetTokenPrincipalAsync();
}
