using System.Collections.Immutable;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Auth;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Base.UserContext;
using PublicSafetyHub.Data.Context;
using PublicSafetyHub.Data.Repositories.Interfaces;
using PublicSafetyHub.Models.Auth.Requests;
using PublicSafetyHub.Models.Auth.Responses;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace PublicSafetyHub.Application.Services;

public class AuthService(
    UserManager<UserEntity> userManager,
    SignInManager<UserEntity> signInManager,
    IPublicSafetyHubDbContext dbContext,
    IUserRepository userRepository,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictTokenManager tokenManager,
    IHttpUserContext httpUserContext,
    IValidator<RegisterUserRequest> registerUserRequestValidator,
    IValidator<DeleteUserRequest> deleteUserRequestValidator)
    : IAuthService
{
    public async Task<ApplicationResult<UserResponse>> RegisterUserAsync(RegisterUserRequest registerUserRequest)
    {
        var validationResult = await registerUserRequestValidator.ValidateAsync(registerUserRequest);

        if (!validationResult.IsValid)
        {
            return ResultHelper.Validation(validationResult.Errors[0].ErrorMessage);
        }

        var email = registerUserRequest.Email.Trim();
        var normalizedEmail = userManager.NormalizeEmail(email);
        var emailExists = await userRepository.GetQueryable().IgnoreQueryFilters().AnyAsync(user => user.NormalizedEmail == normalizedEmail);

        if (emailExists)
        {
            return ResultHelper.Validation("An account with this email already exists.");
        }

        var user = new UserEntity { UserName = email, Email = email };
        var result = await userManager.CreateAsync(user, registerUserRequest.Password);

        if (!result.Succeeded)
        {
            return MapIdentityErrors(result);
        }

        return new UserResponse { Id = user.Id, Email = user.Email! };
    }

    public async Task<ApplicationResult<ClaimsPrincipal>> LoginAsync(string email, string password, string clientId, IEnumerable<string> scopes)
    {
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
        var authorization = await authorizationManager.CreateAsync(
            (ClaimsIdentity)principal.Identity, user.Id.ToString(), applicationId, AuthorizationTypes.Permanent, grantedScopes);

        principal.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization));

        return principal;
    }

    public async Task<ApplicationResult<ClaimsPrincipal>> RefreshAsync(ClaimsPrincipal principal, IEnumerable<string> scopes)
    {
        var subject = principal.GetClaim(Claims.Subject);
        var user = int.TryParse(subject, out var userId) ? await userRepository.GetAsync(userId) : null;

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

        return refreshedPrincipal;
    }

    public async Task<ApplicationResult<NoValueResult>> LogoutAsync()
    {
        var sessionId = httpUserContext.GetSessionId();

        if (httpUserContext.GetUserId() is not { } userId || sessionId is null)
        {
            return ResultHelper.Unauthorized("Invalid credentials or session.");
        }

        var authorization = await authorizationManager.FindByIdAsync(sessionId);

        if (authorization is null || await authorizationManager.GetSubjectAsync(authorization) != userId.ToString())
        {
            return ResultHelper.Unauthorized("Invalid credentials or session.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        if (!await authorizationManager.TryRevokeAsync(authorization))
        {
            return ResultHelper.Validation("The session has changed. Please retry signing out.");
        }

        await tokenManager.RevokeByAuthorizationIdAsync(sessionId);
        await transaction.CommitAsync();

        return NoValueResult.Value;
    }

    public async Task<ApplicationResult<NoValueResult>> DeleteUserAsync(DeleteUserRequest deleteUserRequest)
    {
        var validationResult = await deleteUserRequestValidator.ValidateAsync(deleteUserRequest);

        if (!validationResult.IsValid)
        {
            return ResultHelper.Validation(validationResult.Errors[0].ErrorMessage);
        }

        var user = httpUserContext.GetUserId() is not { } userId ? null : await userRepository.GetAsync(userId);

        if (user is null || user.IsDeleted)
        {
            return ResultHelper.Unauthorized("Invalid credentials or session.");
        }

        var passwordResult = await signInManager.CheckPasswordSignInAsync(user, deleteUserRequest.Password, lockoutOnFailure: true);

        if (!passwordResult.Succeeded)
        {
            return ResultHelper.Validation("The account could not be deleted. Check your password.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        userRepository.Delete(user);
        var updateResult = await userManager.UpdateSecurityStampAsync(user);

        if (!updateResult.Succeeded)
        {
            return MapIdentityErrors(updateResult);
        }

        var subject = user.Id.ToString();
        await authorizationManager.RevokeBySubjectAsync(subject);
        await tokenManager.RevokeBySubjectAsync(subject);
        await transaction.CommitAsync();

        return NoValueResult.Value;
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(UserEntity user, IEnumerable<string> scopes)
    {
        var identity = new ClaimsIdentity("OpenIddict", Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Email, user.Email);
        identity.SetClaim(Claims.Name, user.UserName);
        identity.SetClaim(AuthConstants.SecurityStampClaim, await userManager.GetSecurityStampAsync(user));
        identity.SetClaims(Claims.Role, (await userManager.GetRolesAsync(user)).ToImmutableArray());

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(scopes);
        principal.SetResources(AuthConstants.ApiResource);
        principal.SetDestinations(claim => claim.Type is Claims.Subject or Claims.Name or Claims.Email or Claims.Role
            ? [Destinations.AccessToken]
            : []);

        return principal;
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
