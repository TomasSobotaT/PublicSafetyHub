using System.Security.Claims;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Models.Auth.Requests;
using PublicSafetyHub.Base.OpenIddictAuth;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface IAuthService
{
    ApplicationResult<AuthorizationFlowResult> PrepareAuthorization();

    Task<ApplicationResult<AuthorizationFlowResult>> AuthorizeAsync();

    Task<ApplicationResult<ClaimsPrincipal>> LoginAsync(string email, string password, string clientId, IEnumerable<string> scopes);

    Task<ApplicationResult<ClaimsPrincipal>> RefreshAsync(ClaimsPrincipal principal, IEnumerable<string> scopes);

    Task<ApplicationResult<NoValueResult>> LogoutAsync();

    Task<ApplicationResult<NoValueResult>> DeleteUserAsync(DeleteUserRequest deleteUserRequest);
}
