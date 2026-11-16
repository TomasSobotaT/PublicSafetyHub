namespace PublicSafetyHub.Base.Entities.Base;

public interface IBaseEntity
{
    bool IsDeleted { get; set; }
}

public interface IBaseEntity<T> : IBaseEntity
{
    T Id { get; set; }
}
