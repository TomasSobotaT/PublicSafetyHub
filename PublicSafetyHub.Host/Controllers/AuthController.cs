using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OpenIddict.Server.AspNetCore;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Models.Auth.Requests;
using PublicSafetyHub.Models.Auth.Responses;

namespace PublicSafetyHub.Host.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Authorize(Policy = "ApiAccess")]
[EnableRateLimiting("Auth")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
public class AuthController(IAuthService authService, ITokenService tokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("RegisterUser")]
    [ProducesResponseType<User>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterUserAsync(RegisterUserRequest registerUserRequest)
    {
        var result = await authService.RegisterUserAsync(registerUserRequest);

        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpPost("Token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<OAuthErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<OAuthErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TokenAsync()
    {
        var result = await tokenService.ExchangeAsync();
        if (result.IsSuccess)
        {
            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return Forbid(new AuthenticationProperties(new Dictionary<string, string>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = result.Error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = result.ErrorDescription
        }), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("Logout")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LogoutAsync()
    {
        var result = await authService.LogoutAsync();

        return result.ToActionResult();
    }

    [HttpDelete("DeleteUser")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUserAsync(DeleteUserRequest deleteUserRequest)
    {
        var result = await authService.DeleteUserAsync(deleteUserRequest);

        return result.ToActionResult();
    }
}
