using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using PublicSafetyHub.Base.Auth;
using PublicSafetyHub.Base.UserContext;

namespace PublicSafetyHub.Application.UserContext;

public class UserContext(IHttpContextAccessor httpContextAccessor) : HttpUserContext(httpContextAccessor), IUserContext
{
    public string GetSessionId()
    {
        return HttpContext?.User.GetAuthorizationId();
    }

    public OpenIddictRequest GetTokenRequest()
    {
        return GetHttpContext().GetOpenIddictServerRequest() ?? throw new InvalidOperationException("The OAuth request is not available.");
    }

    public AuthorizationLoginSubmission GetAuthorizationSubmission()
    {
        return GetHttpContext().Items.TryGetValue(typeof(AuthorizationLoginSubmission), out var value) ? value as AuthorizationLoginSubmission : null;
    }

    public async Task<ClaimsPrincipal> GetTokenPrincipalAsync()
    {
        var authentication = await GetHttpContext().AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        return authentication.Succeeded ? authentication.Principal : null;
    }
}
