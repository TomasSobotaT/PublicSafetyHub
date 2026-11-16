namespace PublicSafetyHub.Base.Entities.Base;

public interface IExtendedBaseEntity : IBaseEntity
{
    DateTime CreatedAt { get; set; }

    DateTime? UpdatedAt { get; set; }

}

public interface IExtendedBaseEntity<T> : IBaseEntity<T>, IExtendedBaseEntity
{
}
