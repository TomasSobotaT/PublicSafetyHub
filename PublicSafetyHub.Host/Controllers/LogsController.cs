using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Models.Logging;

namespace PublicSafetyHub.Host.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/logs")]
[Authorize(Policy = "ApiAccess", Roles = "Admin")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class LogsController(ILogService logService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<Log>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogsAsync([FromQuery] FilteredRequest<LogFilter> request)
    {
        var result = await logService.GetLogsAsync(request);

        return result.ToActionResult();
    }

    [HttpGet("categories")]
    [ProducesResponseType<PagedResult<string>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoriesAsync([FromQuery] FilteredRequest<LogCategoryFilter> request)
    {
        var result = await logService.GetCategoriesAsync(request);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<Log>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogAsync(Guid id)
    {
        var result = await logService.GetLogAsync(id);
        return result.ToActionResult();
    }
}
