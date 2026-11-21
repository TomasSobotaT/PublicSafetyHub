using System.Security.Claims;

namespace PublicSafetyHub.Base.OpenIddictAuth;

public class AuthorizationFlowResult
{
    private AuthorizationFlowResult(ClaimsPrincipal principal, string error, string errorDescription, string transactionId)
    {
        Principal = principal;
        Error = error;
        ErrorDescription = errorDescription;
        TransactionId = transactionId;
    }

    public ClaimsPrincipal Principal { get; }

    public string Error { get; }

    public string ErrorDescription { get; }

    public string TransactionId { get; }

    public static AuthorizationFlowResult Interaction(string transactionId) => new(null, null, null, transactionId);

    public static AuthorizationFlowResult Success(ClaimsPrincipal principal) => new(principal, null, null, null);

    public static AuthorizationFlowResult Failure(string error, string description) => new(null, error, description, null);
}
