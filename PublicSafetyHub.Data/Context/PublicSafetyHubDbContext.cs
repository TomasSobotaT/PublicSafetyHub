using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Data.Conventions;

namespace PublicSafetyHub.Data.Context;

public class PublicSafetyHubDbContext(DbContextOptions<PublicSafetyHubDbContext> options)
    : IdentityDbContext<UserEntity, IdentityRole<int>, int>(options), IPublicSafetyHubDbContext
{
    public DbSet<ArticleEntity> Articles { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new DateTimeConvention());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PublicSafetyHubDbContext).Assembly);
    }
}
