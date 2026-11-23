using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Integration.Clients;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class HttpClientExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHttpClientRegistration()
        {
            services.AddHttpClient<IFirefightersArticlesClient, FirefightersArticlesClient>(httpClient =>
            {
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PublicSafetyHub/1.0");
            });

            services.AddHttpClient<IArmyArticlesClient, ArmyArticlesClient>((provider, httpClient) =>
            {
                var armyArticleImportOptions = provider.GetRequiredService<IArmyArticleImportOptions>();
                httpClient.BaseAddress = new Uri(armyArticleImportOptions.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PublicSafetyHub/1.0");
            });

            services.AddHttpClient<IPoliceArticlesClient, PoliceArticlesClient>((provider, httpClient) =>
            {
                var policeArticleImportOptions = provider.GetRequiredService<IPoliceArticleImportOptions>();
                httpClient.BaseAddress = new Uri(policeArticleImportOptions.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PublicSafetyHub/1.0");
            });

            return services;
        }
    }
}
