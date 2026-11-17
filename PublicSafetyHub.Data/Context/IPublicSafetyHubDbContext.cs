using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PublicSafetyHub.Core.Entities;

namespace PublicSafetyHub.Data.Context;

public interface IPublicSafetyHubDbContext
{
    DatabaseFacade Database { get; }

    ChangeTracker ChangeTracker { get; }

    DbSet<UserEntity> Users { get; }

    DbSet<ArticleEntity> Articles { get; }

    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    int SaveChanges();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
