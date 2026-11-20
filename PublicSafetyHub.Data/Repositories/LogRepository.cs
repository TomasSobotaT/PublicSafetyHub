using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Base.Specifications;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Core.Specifications;
using PublicSafetyHub.Data.Context;
using PublicSafetyHub.Base.Filtering;

namespace PublicSafetyHub.Data.Repositories;

public class LogRepository(PublicSafetyHubDbContext dbContext) : ILogRepository
{
    public async Task<PagedResult<TResult>> GetLogsAsync<TResult>(LogSpecification specification, PaginationRequest paginationRequest,
        Expression<Func<LogEntity, TResult>> projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        specification.ApplyPagination(paginationRequest);
        var query = dbContext.Logs.AsNoTracking();
        var totalCount = await SpecificationEvaluator.GetQuery(query, specification, criteriaOnly: true).CountAsync();
        var items = await SpecificationEvaluator.GetQuery(query, specification).Select(projection).ToListAsync();

        return new PagedResult<TResult>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = paginationRequest.PageNumber,
            PageSize = paginationRequest.PageSize
        };
    }

    public async Task<TResult> GetAsync<TResult>(long id, Expression<Func<LogEntity, TResult>> projection)
    {
        ArgumentNullException.ThrowIfNull(projection);

        return await dbContext.Logs.AsNoTracking().Where(log => log.Id == id).Select(projection).FirstOrDefaultAsync();
    }
}
