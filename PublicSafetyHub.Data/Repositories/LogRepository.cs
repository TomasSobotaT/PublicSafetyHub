using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Base.Repositories;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Data.Context;
using PublicSafetyHub.Base.Filtering;

namespace PublicSafetyHub.Data.Repositories;

public class LogRepository(PublicSafetyHubDbContext dbContext) : BaseRepository<LogEntity, Guid>(dbContext), ILogRepository
{
    public async Task<PagedResult<string>> GetCategoriesAsync(string searchValue, PaginationRequest paginationRequest)
    {
        var query = dbContext.Logs
            .AsNoTracking()
            .Where(log => !string.IsNullOrWhiteSpace(log.Category))
            .Select(log => log.Category)
            .Distinct();

        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            query = query.Where(category => category.Contains(searchValue));
        }

        var totalCount = await query.CountAsync();

        var orderedQuery = string.Equals(paginationRequest.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(category => category)
            : query.OrderBy(category => category);

        var items = await orderedQuery
            .Skip(checked((paginationRequest.PageNumber - 1) * paginationRequest.PageSize))
            .Take(paginationRequest.PageSize)
            .ToListAsync();

        return new PagedResult<string>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = paginationRequest.PageNumber,
            PageSize = paginationRequest.PageSize
        };
    }

}
