using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace PublicSafetyHub.Base.UserContext;

public class HttpUserContext(IHttpContextAccessor httpContextAccessor) : IHttpUserContext
{
    protected HttpContext HttpContext => httpContextAccessor.HttpContext;

    public int? GetUserId()
    {
        var subject = HttpContext?.User.FindFirstValue("sub");
        return int.TryParse(subject, out var userId) ? userId : null;
    }

    protected HttpContext GetHttpContext()
    {
        return HttpContext ?? throw new InvalidOperationException("The HTTP context is not available.");
    }
}
