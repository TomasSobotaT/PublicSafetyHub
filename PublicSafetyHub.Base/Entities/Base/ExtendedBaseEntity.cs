namespace PublicSafetyHub.Base.Entities.Base;

public abstract class ExtendedBaseEntity<T> : BaseEntity<T>, IExtendedBaseEntity<T>
{
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
