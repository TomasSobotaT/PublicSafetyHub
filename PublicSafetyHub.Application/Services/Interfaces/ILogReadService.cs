using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Models.Logging;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface ILogReadService
{
    Task<ApplicationResult<PagedResult<LogSummary>>> GetLogsAsync(FilteredRequest<LogFilter> request);

    Task<ApplicationResult<Log>> GetLogAsync(long id);
}
