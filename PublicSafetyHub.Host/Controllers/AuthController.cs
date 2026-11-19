using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Models.Auth.Requests;
using PublicSafetyHub.Models.Auth.Responses;
using PublicSafetyHub.Host.Extensions;

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
    [ApiVersionNeutral]
    [HttpGet("~/connect/authorize")]
    [ProducesResponseType<AuthorizationTransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult PrepareAuthorization()
    {
        var result = authService.PrepareAuthorization();
        return result.ToOpenIddictActionResult();
    }

    [AllowAnonymous]
    [ApiVersionNeutral]
    [HttpPost("~/connect/authorize")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AuthorizeAsync()
    {
        var result = await authService.AuthorizeAsync();
        return result.ToOpenIddictActionResult();
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
        return result.ToOpenIddictActionResult();
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
