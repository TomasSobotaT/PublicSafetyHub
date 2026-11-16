using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace PublicSafetyHub.Base.UserContext;

public class HttpUserContext(IHttpContextAccessor httpContextAccessor) : IHttpUserContext
{
    public int? GetUserId()
    {
        var subject = httpContextAccessor.HttpContext?.User.GetClaim(OpenIddictConstants.Claims.Subject);

        return int.TryParse(subject, out var userId) ? userId : null;
    }

    public string GetSessionId()
    {
        return httpContextAccessor.HttpContext?.User.GetAuthorizationId();
    }

    public OpenIddictRequest GetTokenRequest()
    {
        return GetHttpContext().GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OAuth request is not available.");
    }

    public async Task<ClaimsPrincipal> GetRefreshTokenPrincipalAsync()
    {
        var authentication = await GetHttpContext().AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        return authentication.Succeeded ? authentication.Principal : null;
    }

    private HttpContext GetHttpContext()
    {
        return httpContextAccessor.HttpContext ?? throw new InvalidOperationException("The HTTP context is not available.");
    }
}
