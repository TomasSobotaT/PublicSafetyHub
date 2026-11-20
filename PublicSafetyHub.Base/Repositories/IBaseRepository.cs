using System.Linq.Expressions;
using PublicSafetyHub.Base.Entities.Base;
using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Specifications;

namespace PublicSafetyHub.Base.Repositories;

public interface IBaseRepository<TEntity, T> where TEntity : class, IBaseEntity<T>
{
    Task AddAsync(TEntity entity);

    void Update(TEntity entity);

    void Delete(TEntity entity);

    Task<TEntity> GetAsync(T id);

    Task<List<TEntity>> GetAllAsync(ISpecification<TEntity> specification = null);

    Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity> specification, PaginationRequest paginationRequest, bool asNoTracking = true, bool ignoreQueryFilters = false);

    Task<PagedResult<TResult>> GetPagedAsync<TResult>(ISpecification<TEntity> specification, PaginationRequest paginationRequest, Expression<Func<TEntity, TResult>> projection = null, bool asNoTracking = true, bool ignoreQueryFilters = false);
}
