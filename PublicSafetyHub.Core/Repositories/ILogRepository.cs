using PublicSafetyHub.Core.Specifications;
using System.Linq.Expressions;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Base.Filtering;

namespace PublicSafetyHub.Core.Repositories;

public interface ILogRepository
{
    Task<PagedResult<TResult>> GetLogsAsync<TResult>(LogSpecification specification, PaginationRequest paginationRequest,
        Expression<Func<LogEntity, TResult>> projection);

    Task<TResult> GetAsync<TResult>(long id, Expression<Func<LogEntity, TResult>> projection);
}
