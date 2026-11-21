using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Repositories;

namespace PublicSafetyHub.Core.Repositories;

public interface ILogRepository : IBaseRepository<LogEntity, Guid>
{
    Task<PagedResult<string>> GetCategoriesAsync(string searchValue, PaginationRequest paginationRequest);
}
