using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.ML.Tokenizers;
using OpenAI;
using PublicSafetyHub.AI.Services;
using PublicSafetyHub.AI.Services.Interfaces;
using PublicSafetyHub.Core.Configurations;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class AiServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAiRegistration()
        {
            services.TryAddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(provider =>
            {
                var options = provider.GetRequiredService<IOpenAiOptions>();
                ArgumentException.ThrowIfNullOrWhiteSpace(options.ApiKey);
                ArgumentException.ThrowIfNullOrWhiteSpace(options.EmbeddingModel);

                return new OpenAIClient(options.ApiKey)
                    .GetEmbeddingClient(options.EmbeddingModel)
                    .AsIEmbeddingGenerator();
            });

            services.TryAddSingleton<Tokenizer>(provider =>
            {
                var options = provider.GetRequiredService<IOpenAiOptions>();
                ArgumentException.ThrowIfNullOrWhiteSpace(options.EmbeddingModel);

                return TiktokenTokenizer.CreateForModel(options.EmbeddingModel);
            });

            services.TryAddScoped<IEmbeddingService, EmbeddingService>();

            return services;
        }
    }
}
