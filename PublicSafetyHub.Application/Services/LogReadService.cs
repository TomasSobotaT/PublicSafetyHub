using AutoMapper;
using FluentValidation;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Application.Projections;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Core.Specifications;
using PublicSafetyHub.Models.Logging;
using PublicSafetyHub.Base.Filtering;

namespace PublicSafetyHub.Application.Services;

public class LogReadService(
    ILogRepository repository,
    IMapper mapper,
    IValidator<PaginationRequest> paginationValidator,
    IValidator<LogFilter> filterValidator) : ILogReadService
{
    public async Task<ApplicationResult<PagedResult<LogSummary>>> GetLogsAsync(FilteredRequest<LogFilter> request)
    {
        var paginationValidation = await paginationValidator.ValidateAsync(request);

        if (!paginationValidation.IsValid)
        {
            return ResultHelper.Validation(paginationValidation.Errors[0].ErrorMessage);
        }

        if (request.Filter is not null)
        {
            var filterValidation = await filterValidator.ValidateAsync(request.Filter);

            if (!filterValidation.IsValid)
            {
                return ResultHelper.Validation(filterValidation.Errors[0].ErrorMessage);
            }
        }

        var specification = request.Filter is null ? new LogSpecification() : mapper.Map<LogSpecification>(request.Filter);

        return await repository.GetLogsAsync(specification, request, LogProjection.Summary);
    }

    public async Task<ApplicationResult<Log>> GetLogAsync(long id)
    {
        var log = await repository.GetAsync(id, LogProjection.Detail);

        if (log is null)
        {
            return ResultHelper.NotFound("Log was not found.");
        }

        return log;
    }
}
