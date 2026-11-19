using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Base.Entities.Base;

namespace PublicSafetyHub.Base.Repositories;

public abstract class ExtendedBaseRepository<TEntity, T>(DbContext dbContext)
    : BaseRepository<TEntity, T>(dbContext), IExtendedBaseRepository<TEntity, T>
    where TEntity : class, IExtendedBaseEntity<T>
{
    public override async Task AddAsync(TEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = null;
        await base.AddAsync(entity);
    }

    public override async Task AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = null;
        await base.AddAsync(entity, cancellationToken);
    }

    public override void Update(TEntity entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        base.Update(entity);
    }
}
