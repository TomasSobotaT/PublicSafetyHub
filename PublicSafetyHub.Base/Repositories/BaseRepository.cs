using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Base.Entities.Base;
using PublicSafetyHub.Base.Pagination;
using PublicSafetyHub.Base.Specifications;

namespace PublicSafetyHub.Base.Repositories;

public abstract class BaseRepository<TEntity, T>(DbContext dbContext) : IBaseRepository<TEntity, T>
    where TEntity : class, IBaseEntity<T>
{
    protected readonly DbContext dbContext = dbContext;
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
        return await GetQueryable().FirstOrDefaultAsync(entity => entity.Id.Equals(id));
    }

    public virtual async Task<List<TEntity>> GetAllAsync(ISpecification<TEntity> specification = null)
    {
        var query = ApplySpecification(GetQueryable(), specification);

        return await query.ToListAsync();
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, ISpecification<TEntity> specification = null)
    {
        var query = ApplySpecification(GetQueryable(), specification);
        var totalCount = await query.CountAsync();
        var offset = checked((pageNumber - 1) * pageSize);
        var items = await query.Skip(offset).Take(pageSize).ToListAsync();

        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
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

    public virtual int Commit()
    {
        return dbContext.SaveChanges();
    }

    public virtual Task<int> CommitAsync()
    {
        return dbContext.SaveChangesAsync();
    }

    protected virtual IQueryable<TEntity> ApplySpecification(IQueryable<TEntity> query, ISpecification<TEntity> specification)
    {
        if (specification?.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        if (specification is not null)
        {
            foreach (var include in specification.Includes)
            {
                query = query.Include(include);
            }
        }

        if (specification?.OrderBy is not null)
        {
            var orderedQuery = specification.OrderDescending
                ? query.OrderByDescending(specification.OrderBy)
                : query.OrderBy(specification.OrderBy);

            return orderedQuery.ThenBy(entity => entity.Id);
        }

        return query.OrderBy(entity => entity.Id);
    }
}
