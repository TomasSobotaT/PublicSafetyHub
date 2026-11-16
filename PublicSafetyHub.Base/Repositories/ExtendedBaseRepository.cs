using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Base.Entities.Base;

namespace PublicSafetyHub.Base.Repositories;

public abstract class ExtendedBaseRepository<TEntity, T>(DbContext dbContext)
    : BaseRepository<TEntity, T>(dbContext), IExtendedBaseRepository<TEntity, T>
    where TEntity : class, IExtendedBaseEntity<T>
{
    public override async Task AddAsync(TEntity entity)
    {
        entity.CreatedAt = DateTime.Now;
        entity.UpdatedAt = null;
        await base.AddAsync(entity);
    }

    public override void Update(TEntity entity)
    {
        entity.UpdatedAt = DateTime.Now;
        base.Update(entity);
    }
}
