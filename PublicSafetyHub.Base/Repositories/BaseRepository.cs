using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Base.Entities.Base;
using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Pagination;
using PublicSafetyHub.Base.Specifications;

namespace PublicSafetyHub.Base.Repositories;

public abstract class BaseRepository<TEntity, T>(DbContext dbContext) : IBaseRepository<TEntity, T>
    where TEntity : class, IBaseEntity<T>
{
    protected readonly DbSet<TEntity> dbSet = dbContext.Set<TEntity>();

    public virtual async Task AddAsync(TEntity entity)
    {
        await GetDbSet().AddAsync(entity);
    }

    public virtual void Update(TEntity entity)
    {
        GetDbSet().Update(entity);
    }

    public virtual void Delete(TEntity entity)
    {
        entity.IsDeleted = true;
        Update(entity);
    }

    public virtual async Task<TEntity> GetAsync(T id)
    {
        return await GetQueryable().AsNoTracking().FirstOrDefaultAsync(entity => entity.Id.Equals(id));
    }

    public virtual async Task<List<TEntity>> GetAllAsync(ISpecification<TEntity> specification = null)
    {
        var query = ApplySpecification(GetQueryable().AsNoTracking().OrderBy(entity => entity.Id), specification);

        return await query.ToListAsync();
    }

    public virtual Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity> specification, PaginationRequest paginationRequest, bool asNoTracking = true, bool ignoreQueryFilters = false)
    {
        return GetPagedAsync<TEntity>(specification, paginationRequest, asNoTracking: asNoTracking, ignoreQueryFilters: ignoreQueryFilters);
    }

    public virtual async Task<PagedResult<TResult>> GetPagedAsync<TResult>(ISpecification<TEntity> specification, PaginationRequest paginationRequest, Expression<Func<TEntity, TResult>> projection = null, bool asNoTracking = true, bool ignoreQueryFilters = false)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(paginationRequest);

        if (projection is null && typeof(TResult) != typeof(TEntity))
        {
            throw new ArgumentException("A projection is required when the result type differs from the entity type.", nameof(projection));
        }

        specification.ApplyPagination(paginationRequest);

        var query = GetQueryable();
        query = asNoTracking ? query.AsNoTracking() : query.AsTracking();

        if (ignoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        query = query.OrderBy(entity => entity.Id);

        var filteredQuery = SpecificationEvaluator.GetQuery(query, specification, criteriaOnly: true);
        var totalCount = await filteredQuery.CountAsync();
        var pagedQuery = ApplySpecification(query, specification);
        var resultQuery = projection is null
            ? (IQueryable<TResult>)(object)pagedQuery
            : pagedQuery.Select(projection);

        var items = await resultQuery.ToListAsync();

        return new PagedResult<TResult>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = specification.PageNumber.Value,
            PageSize = specification.PageSize.Value
        };
    }

    protected virtual DbSet<TEntity> GetDbSet()
    {
        return dbSet;
    }

    protected virtual IQueryable<TEntity> GetQueryable()
    {
        return GetDbSet().AsQueryable();
    }

    protected virtual IQueryable<TEntity> ApplySpecification(IQueryable<TEntity> query, ISpecification<TEntity> specification)
    {
        return SpecificationEvaluator.GetQuery(query, specification);
    }
}
