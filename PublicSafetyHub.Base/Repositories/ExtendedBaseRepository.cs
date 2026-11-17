using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Base.Entities.Base;
using PublicSafetyHub.Base.Time;

namespace PublicSafetyHub.Base.Repositories;

public abstract class ExtendedBaseRepository<TEntity, T>(DbContext dbContext)
    : BaseRepository<TEntity, T>(dbContext), IExtendedBaseRepository<TEntity, T>
    where TEntity : class, IExtendedBaseEntity<T>
{
    public override async Task AddAsync(TEntity entity)
    {
        entity.CreatedAt = CzechTime.Now;
        entity.UpdatedAt = null;
        await base.AddAsync(entity);
    }

    public override void Update(TEntity entity)
    {
        entity.UpdatedAt = CzechTime.Now;
        base.Update(entity);
    }
}
