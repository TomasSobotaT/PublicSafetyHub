using PublicSafetyHub.Base.Entities.Base;

namespace PublicSafetyHub.Base.Repositories;

public interface IExtendedBaseRepository<TEntity, T> : IBaseRepository<TEntity, T>
    where TEntity : class, IExtendedBaseEntity<T>
{
}
