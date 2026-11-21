using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Models.Users;

namespace PublicSafetyHub.Host.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/users")]
[Authorize(Policy = "ApiAccess", Roles = "Admin")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<User>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsersAsync([FromQuery] FilteredRequest<UserFilter> request)
    {
        var result = await userService.GetUsersAsync(request);
        return result.ToActionResult();
    }

    [HttpPut("{id:int}/blocked")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BlockUserAsync(int id, [FromBody] BlockUserRequest request)
    {
        var result = await userService.BlockUserAsync(id, request);
        return result.ToActionResult();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUserAsync(int id)
    {
        var result = await userService.DeleteUserAsync(id);
        return result.ToActionResult();
    }
}
