using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Integration.Clients;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class HttpClientExtension
{
    public static IServiceCollection AddHttpClientRegistration(this IServiceCollection services)
    {
        services.AddHttpClient<IArmyArticlesClient, ArmyArticlesClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IArmyArticleImportOptions>();
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PublicSafetyHub/1.0");
        });

        services.AddHttpClient<IPoliceArticlesClient, PoliceArticlesClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IPoliceArticleImportOptions>();
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PublicSafetyHub/1.0");
        });

        return services;
    }
}
