using System.Security.Claims;
using OpenIddict.Abstractions;

namespace PublicSafetyHub.Base.UserContext;

public interface IHttpUserContext
{
    int? GetUserId();

    string GetSessionId();

    OpenIddictRequest GetTokenRequest();

    Task<ClaimsPrincipal> GetRefreshTokenPrincipalAsync();
}
