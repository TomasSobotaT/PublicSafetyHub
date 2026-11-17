using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Base.Entities.Base;
using PublicSafetyHub.Base.Pagination;
using PublicSafetyHub.Base.Specifications;

namespace PublicSafetyHub.Base.Repositories;

public interface IBaseRepository<TEntity, T> where TEntity : class, IBaseEntity<T>
{
    Task AddAsync(TEntity entity);

    void Update(TEntity entity);

    void Delete(TEntity entity);

    Task<TEntity> GetAsync(T id, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetAllAsync(ISpecification<TEntity> specification = null, CancellationToken cancellationToken = default);

    Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

    Task<PagedResult<TResult>> GetPagedAsync<TResult>(ISpecification<TEntity> specification, Func<IQueryable<TEntity>, IQueryable<TResult>> projection, CancellationToken cancellationToken = default);

    DbSet<TEntity> GetDbSet();

    IQueryable<TEntity> GetQueryable();
}
