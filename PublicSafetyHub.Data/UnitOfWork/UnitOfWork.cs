using Microsoft.EntityFrameworkCore.Storage;
using PublicSafetyHub.Base.UnitOfWork;
using PublicSafetyHub.Data.Context;

namespace PublicSafetyHub.Data.UnitOfWork;

public class UnitOfWork(IPublicSafetyHubDbContext dbContext) : IUnitOfWork
{
    private IDbContextTransaction transaction;

    public Task<int> SaveChangesAsync()
    {
        return dbContext.SaveChangesAsync();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        if (transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active.");
        }

        transaction = await dbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (transaction is null)
        {
            throw new InvalidOperationException("No transaction is active.");
        }

        await SaveChangesAsync();
        await transaction.CommitAsync();
        await DisposeTransactionAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        if (transaction is null)
        {
            return;
        }

        try
        {
            await transaction.RollbackAsync();
        }
        finally
        {
            dbContext.ChangeTracker.Clear();
            await DisposeTransactionAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await RollbackTransactionAsync();
    }

    private async Task DisposeTransactionAsync()
    {
        var completedTransaction = transaction;
        transaction = null;
        await completedTransaction.DisposeAsync();
    }
}
