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

    public virtual async Task<TEntity> GetAsync(T id, CancellationToken cancellationToken = default)
    {
        return await GetQueryable().AsNoTracking().FirstOrDefaultAsync(entity => entity.Id.Equals(id), cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetAllAsync(ISpecification<TEntity> specification = null, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(GetQueryable().AsNoTracking().OrderBy(entity => entity.Id), specification);

        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity> specification, PaginationRequest paginationRequest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(paginationRequest);
        specification.ApplyPagination(paginationRequest);

        var query = GetQueryable().AsNoTracking().OrderBy(entity => entity.Id);
        var filteredQuery = SpecificationEvaluator.GetQuery(query, specification, criteriaOnly: true);
        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        var pagedQuery = ApplySpecification(query, specification);
        var items = await pagedQuery.ToListAsync(cancellationToken);

        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = specification.PageNumber.Value,
            PageSize = specification.PageSize.Value
        };
    }

    public virtual DbSet<TEntity> GetDbSet()
    {
        return dbSet;
    }

    public virtual IQueryable<TEntity> GetQueryable()
    {
        return GetDbSet().AsQueryable();
    }

    protected virtual IQueryable<TEntity> ApplySpecification(IQueryable<TEntity> query, ISpecification<TEntity> specification)
    {
        return SpecificationEvaluator.GetQuery(query, specification);
    }
}
