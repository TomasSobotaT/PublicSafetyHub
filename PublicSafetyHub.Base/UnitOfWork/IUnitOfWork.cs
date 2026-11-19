namespace PublicSafetyHub.Base.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    Task<int> SaveChangesAsync();

    Task BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();
}
