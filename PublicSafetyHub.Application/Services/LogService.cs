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

public class LogService(
    ILogRepository repository,
    IMapper mapper,
    IValidator<PaginationRequest> paginationValidator,
    IValidator<LogFilter> filterValidator) : ILogService
{
    public async Task<ApplicationResult<PagedResult<Log>>> GetLogsAsync(FilteredRequest<LogFilter> request)
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

        return await repository.GetPagedAsync(specification, request, projection: LogProjection.Log);
    }

    public async Task<ApplicationResult<PagedResult<string>>> GetCategoriesAsync(FilteredRequest<LogCategoryFilter> request)
    {
        var validation = await paginationValidator.ValidateAsync(request);

        if (!validation.IsValid)
        {
            return ResultHelper.Validation(validation.Errors[0].ErrorMessage);
        }

        if (request.Filter?.SearchValue?.Length > 512)
        {
            return ResultHelper.Validation("SearchValue must not exceed 512 characters.");
        }

        return await repository.GetCategoriesAsync(request.Filter?.SearchValue, request);
    }

    public async Task<ApplicationResult<Log>> GetLogAsync(Guid id)
    {
        var logEntity = await repository.GetAsync(id);
        return logEntity is null
            ? ResultHelper.NotFound("Log was not found.")
            : mapper.Map<Log>(logEntity);
    }
}
