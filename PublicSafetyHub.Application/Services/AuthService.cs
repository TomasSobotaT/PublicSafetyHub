using System.Collections.Immutable;
using System.Security.Claims;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.OpenIddictAuth;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Application.UserContext;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Models.Auth.Requests;
using static OpenIddict.Abstractions.OpenIddictConstants;
using PublicSafetyHub.Models.Users;

namespace PublicSafetyHub.Application.Services;

public class AuthService(
    UserManager<UserEntity> userManager,
    SignInManager<UserEntity> signInManager,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictTokenManager tokenManager,
    IUserContext httpUserContext,
    IValidator<RegisterUserRequest> registerUserRequestValidator,
    IValidator<DeleteUserRequest> deleteUserRequestValidator,
    IAuthOptions authOptions,
    IAuthTransactionService authTransactionService)
    : IAuthService
{
    public ApplicationResult<AuthorizationFlowResult> PrepareAuthorization()
    {
        var request = httpUserContext.GetTokenRequest();
        var validation = ValidateAuthorizationRequest(request);
        return validation is not null ? validation : CreateAuthorizationTransaction(request);
    }

    private AuthorizationFlowResult ValidateAuthorizationRequest(OpenIddictRequest request)
    {
        var client = authOptions.Clients.SingleOrDefault(client => client.Enabled && string.Equals(client.ClientId, request.ClientId, StringComparison.Ordinal));

        if (client is null)
        {
            return AuthorizationFlowResult.Failure(Errors.UnauthorizedClient, "The client is not enabled.");
        }

        if (string.IsNullOrWhiteSpace(request.RedirectUri) || request.GetParameters().Any(parameter => parameter.Key is "username" or "password" or "email" or "transaction_id" or "intent" or "confirm_password"))
        {
            return AuthorizationFlowResult.Failure(Errors.InvalidRequest, "A redirect_uri is required and credentials must only be submitted in the authorization POST body.");
        }

        if (!request.HasScope(AuthConstants.ApiScope) || request.GetScopes().Any(scope => !client.Scopes.Contains(scope, StringComparer.Ordinal)))
        {
            return AuthorizationFlowResult.Failure(Errors.InvalidScope, "The API scope is required and all requested scopes must be allowed for this client.");
        }

        return request.HasPromptValue(PromptValues.None)
            ? AuthorizationFlowResult.Failure(Errors.LoginRequired, "Interactive authentication is required.")
            : null;
    }

    public async Task<ApplicationResult<AuthorizationFlowResult>> AuthorizeAsync()
    {
        var submission = httpUserContext.GetAuthorizationSubmission();

        if (submission is null || submission.Intent is not ("login" or "register"))
        {
            return ResultHelper.Validation("Invalid authorization request.");
        }

        var request = httpUserContext.GetTokenRequest();
        var validation = ValidateAuthorizationRequest(request);

        if (validation is not null)
        {
            return validation;
        }

        var register = submission.Intent == "register";

        if (register)
        {
            var registration = await RegisterUserAsync(new RegisterUserRequest
            {
                Email = submission.Email,
                Password = submission.Password,
                ConfirmPassword = submission.ConfirmPassword
            });

            if (!registration.IsSuccess)
            {
                return registration.Error;
            }
        }

        var login = await LoginAsync(submission.Email, submission.Password, request.ClientId, request.GetScopes());

        if (!login.IsSuccess)
        {
            return register
                ? ResultHelper.Unauthorized("The account was created, but sign-in failed. Please try signing in.")
                : login.Error;
        }

        return AuthorizationFlowResult.Success(login.Value);
    }

    private ApplicationResult<AuthorizationFlowResult> CreateAuthorizationTransaction(OpenIddictRequest request)
    {
        var id = authTransactionService.CreateTransaction(request);

        if (id is null)
        {
            return ResultHelper.Unavailable("Authorization is temporarily unavailable. Please try again later.");
        }

        return AuthorizationFlowResult.Interaction(id);
    }

    private async Task<ApplicationResult<User>> RegisterUserAsync(RegisterUserRequest registerUserRequest)
    {
        var validationResult = await registerUserRequestValidator.ValidateAsync(registerUserRequest);

        if (!validationResult.IsValid)
        {
            return ResultHelper.Validation(validationResult.Errors[0].ErrorMessage);
        }

        var email = registerUserRequest.Email.Trim();
        var normalizedEmail = userManager.NormalizeEmail(email);
        var emailExists = await userRepository.UserExistsByEmailAsync(normalizedEmail);

        if (emailExists)
        {
            return ResultHelper.Validation("An account with this email already exists.");
        }

        var user = new UserEntity { UserName = email, Email = email };
        var result = await userRepository.CreateUserAsync(user, registerUserRequest.Password);

        if (!result.Succeeded)
        {
            return MapIdentityErrors(result);
        }

        return mapper.Map<User>(user);
    }

    public async Task<ApplicationResult<ClaimsPrincipal>> LoginAsync(string email, string password, string clientId, IEnumerable<string> scopes)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256 || string.IsNullOrWhiteSpace(password) || password.Length > 128)
        {
            return ResultHelper.Unauthorized("Invalid credentials or session.");
        }

        var normalizedEmail = userManager.NormalizeEmail(email.Trim());
        var user = await userRepository.GetUserByEmailAsync(normalizedEmail);

        if (user is null || user.IsDeleted || user.IsBlocked)
        {
            return ResultHelper.Unauthorized("Invalid credentials or session.");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return ResultHelper.Unauthorized("Invalid credentials or session.");
        }

        var client = await applicationManager.FindByClientIdAsync(clientId) ?? throw new InvalidOperationException("The validated OAuth client could not be found.");
        var applicationId = await applicationManager.GetIdAsync(client);
        var grantedScopes = scopes.ToImmutableArray();
        var principal = await CreatePrincipalAsync(user, grantedScopes);
        var authorization = await authorizationManager.CreateAsync((ClaimsIdentity)principal.Identity, user.Id.ToString(), applicationId, AuthorizationTypes.Permanent, grantedScopes);

        var authorizationId = await authorizationManager.GetIdAsync(authorization);
        principal.SetAuthorizationId(authorizationId);

        return principal;
    }

    public async Task<ApplicationResult<ClaimsPrincipal>> RefreshAsync(ClaimsPrincipal principal, IEnumerable<string> scopes)
    {
        var subject = principal.GetClaim(Claims.Subject);
        var userId = int.TryParse(subject, out var parsedUserId) ? parsedUserId : (int?)null;

        var user = userId.HasValue ? await userRepository.GetAsync(userId.Value) : null;

        if (user is null || user.IsDeleted || user.IsBlocked || await userManager.IsLockedOutAsync(user))
        {
            return ResultHelper.Unauthorized("Invalid credentials or session.");
        }

        if (principal.GetClaim(AuthConstants.SecurityStampClaim) != await userManager.GetSecurityStampAsync(user))
        {
            return ResultHelper.Unauthorized("Invalid credentials or session.");
        }

        var sessionId = principal.GetAuthorizationId();
        var authorization = sessionId is null ? null : await authorizationManager.FindByIdAsync(sessionId);

        if (authorization is null || !await authorizationManager.HasStatusAsync(authorization, Statuses.Valid))
        {
            return ResultHelper.Unauthorized("Invalid credentials or session.");
        }

        var refreshedPrincipal = await CreatePrincipalAsync(user, scopes);
        refreshedPrincipal.SetAuthorizationId(sessionId);

        principal.SetScopes(refreshedPrincipal.GetScopes());
        principal.SetResources(refreshedPrincipal.GetResources());

        foreach (var type in new[] { Claims.Subject, Claims.Name, Claims.Email, Claims.Role, AuthConstants.SecurityStampClaim })
        {
            principal.SetClaims(type, refreshedPrincipal.GetClaims(type));
        }

        SetClaimDestinations(principal);

        return principal;
    }

    public async Task<ApplicationResult<NoValueResult>> LogoutAsync()
    {
        var sessionId = httpUserContext.GetSessionId();
        var userId = httpUserContext.GetUserId();

        if (!userId.HasValue || sessionId is null)
        {
            return ResultHelper.Unauthorized("Invalid credentials or session.");
        }

        var authorization = await authorizationManager.FindByIdAsync(sessionId);

        if (authorization is null || await authorizationManager.GetSubjectAsync(authorization) != userId.Value.ToString())
        {
            return ResultHelper.Unauthorized("Invalid credentials or session.");
        }

        await unitOfWork.BeginTransactionAsync();

        if (!await authorizationManager.TryRevokeAsync(authorization))
        {
            await unitOfWork.RollbackTransactionAsync();

            return ResultHelper.Validation("The session has changed. Please retry signing out.");
        }

        await tokenManager.RevokeByAuthorizationIdAsync(sessionId);
        await unitOfWork.CommitTransactionAsync();

        return NoValueResult.Value;
    }

    public async Task<ApplicationResult<NoValueResult>> DeleteUserAsync(DeleteUserRequest deleteUserRequest)
    {
        var validationResult = await deleteUserRequestValidator.ValidateAsync(deleteUserRequest);

        if (!validationResult.IsValid)
        {
            return ResultHelper.Validation(validationResult.Errors[0].ErrorMessage);
        }

        var userId = httpUserContext.GetUserId();

        if (!userId.HasValue)
        {
            return ResultHelper.Unauthorized("Invalid credentials or session.");
        }

        var user = await userRepository.GetAsync(userId.Value);

        if (user is null || user.IsDeleted)
        {
            return ResultHelper.Unauthorized("Invalid credentials or session.");
        }

        var passwordResult = await signInManager.CheckPasswordSignInAsync(user, deleteUserRequest.Password, lockoutOnFailure: true);

        if (!passwordResult.Succeeded)
        {
            return ResultHelper.Validation("The account could not be deleted. Check your password.");
        }

        await unitOfWork.BeginTransactionAsync();
        userRepository.Delete(user);
        var updateResult = await userManager.UpdateSecurityStampAsync(user);

        if (!updateResult.Succeeded)
        {
            await unitOfWork.RollbackTransactionAsync();
            return MapIdentityErrors(updateResult);
        }

        var subject = user.Id.ToString();
        await authorizationManager.RevokeBySubjectAsync(subject);
        await tokenManager.RevokeBySubjectAsync(subject);
        await unitOfWork.CommitTransactionAsync();

        return NoValueResult.Value;
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(UserEntity user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity("OpenIddict", Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Email, user.Email);
        identity.SetClaim(Claims.Name, user.UserName);
        var securityStamp = await userManager.GetSecurityStampAsync(user);

        identity.SetClaim(AuthConstants.SecurityStampClaim, securityStamp);
        var roles = await userManager.GetRolesAsync(user);
        identity.SetClaims(Claims.Role, [.. roles]);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);
        principal.SetResources(AuthConstants.ApiResource);
        SetClaimDestinations(principal);

        return principal;
    }

    private static void SetClaimDestinations(ClaimsPrincipal principal)
    {
        principal.SetDestinations(claim => claim.Type switch
        {
            Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Role when principal.HasScope(Scopes.Roles) => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Name or Claims.Email or Claims.Role => [Destinations.AccessToken],
            _ => []
        });
    }

    private static ApplicationError MapIdentityErrors(IdentityResult result)
    {
        if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
        {
            return ResultHelper.Validation("An account with this email already exists.");
        }

        if (result.Errors.Any(error => error.Code == "ConcurrencyFailure"))
        {
            return ResultHelper.Validation("The account has changed. Please retry the request.");
        }

        var message = result.Errors.Select(error => error.Description).FirstOrDefault() ?? "The request does not meet the account requirements.";

        return ResultHelper.Validation(message);
    }
}
