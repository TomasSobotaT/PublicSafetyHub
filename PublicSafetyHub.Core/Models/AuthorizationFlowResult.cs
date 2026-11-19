using System.Security.Claims;
using PublicSafetyHub.Models.Auth.Responses;

namespace PublicSafetyHub.Core.Models;

public class AuthorizationFlowResult
{
    private AuthorizationFlowResult(ClaimsPrincipal principal, string error, string errorDescription, AuthorizationTransactionResponse transaction)
    {
        Principal = principal;
        Error = error;
        ErrorDescription = errorDescription;
        Transaction = transaction;
    }

    public ClaimsPrincipal Principal { get; }
    public string Error { get; }
    public string ErrorDescription { get; }
    public AuthorizationTransactionResponse Transaction { get; }

    public static AuthorizationFlowResult Interaction(AuthorizationTransactionResponse transaction) => new(null, null, null, transaction);
    public static AuthorizationFlowResult Success(ClaimsPrincipal principal) => new(principal, null, null, null);
    public static AuthorizationFlowResult Failure(string error, string description) => new(null, error, description, null);
}
