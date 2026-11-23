using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenSearch.Client;
using PublicSafetyHub.Core.Configurations;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class OpenSearchServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOpenSearchRegistration()
        {
            services.TryAddSingleton<IOpenSearchClient>(provider =>
            {
                var openSearchOptions = provider.GetRequiredService<IOpenSearchOptions>();

                if (!Uri.TryCreate(openSearchOptions.Url, UriKind.Absolute, out var url))
                {
                    throw new InvalidOperationException("OpenSearchOptions:Url must be an HTTPS service address without credentials, path, query or fragment.");
                }

                var settings = new ConnectionSettings(url).BasicAuthentication(openSearchOptions.Username, openSearchOptions.Password);

                return new OpenSearchClient(settings);
            });

            return services;
        }
    }
}
