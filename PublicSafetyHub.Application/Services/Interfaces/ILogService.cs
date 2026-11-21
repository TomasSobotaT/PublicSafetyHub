using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Models.Logging;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface ILogService
{
    Task<ApplicationResult<PagedResult<Log>>> GetLogsAsync(FilteredRequest<LogFilter> request);

    Task<ApplicationResult<PagedResult<string>>> GetCategoriesAsync(FilteredRequest<LogCategoryFilter> request);

    Task<ApplicationResult<Log>> GetLogAsync(Guid id);
}
