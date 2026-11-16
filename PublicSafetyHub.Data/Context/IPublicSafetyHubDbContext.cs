using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PublicSafetyHub.Core.Entities;

namespace PublicSafetyHub.Data.Context;

public interface IPublicSafetyHubDbContext
{
    DatabaseFacade Database { get; }

    DbSet<UserEntity> Users { get; }

    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    int SaveChanges();

    Task<int> SaveChangesAsync();
}
