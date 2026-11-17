using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PublicSafetyHub.Data.Context;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class DatabaseServiceCollectionExtension
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Set ConnectionStrings:DefaultConnection using user secrets or environment variables.");
       
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<PublicSafetyHubDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseOpenIddict();
        });

        services.TryAddScoped<IPublicSafetyHubDbContext>(provider => provider.GetRequiredService<PublicSafetyHubDbContext>());

        services.AddOpenIddict().AddCore(options =>
        {
            options.DisableEntityCaching();
            options.UseEntityFrameworkCore().UseDbContext<PublicSafetyHubDbContext>();
        });

        return services;
    }
}
