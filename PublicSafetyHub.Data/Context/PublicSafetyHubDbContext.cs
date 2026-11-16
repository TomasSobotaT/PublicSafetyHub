using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Base.Entities.Base;

namespace PublicSafetyHub.Data.Context;

public class PublicSafetyHubDbContext(DbContextOptions<PublicSafetyHubDbContext> options)
    : IdentityDbContext<UserEntity, IdentityRole<int>, int>(options), IPublicSafetyHubDbContext
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetEntityTimestamps();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken _ = default)
    {
        SetEntityTimestamps();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess);
    }

    public Task<int> SaveChangesAsync()
    {
        return base.SaveChangesAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PublicSafetyHubDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(entityType => typeof(IExtendedBaseEntity).IsAssignableFrom(entityType.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType).Property(nameof(IExtendedBaseEntity.CreatedAt)).HasColumnType("timestamp without time zone");
            modelBuilder.Entity(entityType.ClrType).Property(nameof(IExtendedBaseEntity.UpdatedAt)).HasColumnType("timestamp without time zone");
        }
    }

    private void SetEntityTimestamps()
    {
        var now = DateTime.Now;
        foreach (var entry in ChangeTracker.Entries<IExtendedBaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = null;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
