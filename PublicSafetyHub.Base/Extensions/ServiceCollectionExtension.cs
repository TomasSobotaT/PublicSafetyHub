using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PublicSafetyHub.Base.Extensions;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddConfiguration<TInterface, TConfiguration>(this IServiceCollection services, IConfiguration configuration, Action<TConfiguration> validate = null)
        where TInterface : class
        where TConfiguration : class, TInterface
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var sectionName = typeof(TConfiguration).Name;
        var section = configuration.GetRequiredSection(sectionName);
        var settings = section.Get<TConfiguration>() ?? throw new InvalidOperationException($"Configuration section '{sectionName}' could not be bound to '{typeof(TConfiguration).FullName}'.");

        validate?.Invoke(settings);
        services.AddSingleton<TInterface>(settings);

        return services;
    }
}
