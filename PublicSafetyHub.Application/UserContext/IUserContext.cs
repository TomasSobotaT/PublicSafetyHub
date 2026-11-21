using System.Security.Claims;
using OpenIddict.Abstractions;
using PublicSafetyHub.Base.Auth;
using PublicSafetyHub.Base.UserContext;

namespace PublicSafetyHub.Application.UserContext;

public interface IUserContext : IHttpUserContext
{
    string GetSessionId();

    OpenIddictRequest GetTokenRequest();

    AuthorizationLoginSubmission GetAuthorizationSubmission();

    Task<ClaimsPrincipal> GetTokenPrincipalAsync();
}
