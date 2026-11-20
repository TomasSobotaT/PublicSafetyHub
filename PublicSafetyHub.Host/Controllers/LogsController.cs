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
public class LogsController(ILogReadService logReadService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<LogSummary>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogsAsync([FromQuery] FilteredRequest<LogFilter> request)
    {
        var result = await logReadService.GetLogsAsync(request);

        return result.ToActionResult();
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<Log>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogAsync(long id)
    {
        var result = await logReadService.GetLogAsync(id);

        return result.ToActionResult();
    }
}
