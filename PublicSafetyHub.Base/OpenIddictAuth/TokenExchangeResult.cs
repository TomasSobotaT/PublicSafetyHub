using System.Security.Claims;

namespace PublicSafetyHub.Base.OpenIddictAuth;

public class TokenExchangeResult
{
    private TokenExchangeResult(ClaimsPrincipal principal, string error, string errorDescription)
    {
        Principal = principal;
        Error = error;
        ErrorDescription = errorDescription;
    }

    public bool IsSuccess => Error is null;

    public ClaimsPrincipal Principal { get; }

    public string Error { get; }

    public string ErrorDescription { get; }

    public static TokenExchangeResult Success(ClaimsPrincipal principal) => new(principal, null, null);

    public static TokenExchangeResult Failure(string error, string description) => new(null, error, description);
}
