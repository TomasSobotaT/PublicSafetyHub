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
                var openAiOptions = provider.GetRequiredService<IOpenAiOptions>();
                ArgumentException.ThrowIfNullOrWhiteSpace(openAiOptions.ApiKey);
                ArgumentException.ThrowIfNullOrWhiteSpace(openAiOptions.EmbeddingModel);

                return new OpenAIClient(openAiOptions.ApiKey)
                    .GetEmbeddingClient(openAiOptions.EmbeddingModel)
                    .AsIEmbeddingGenerator();
            });

            services.TryAddSingleton<Tokenizer>(provider =>
            {
                var openAiOptions = provider.GetRequiredService<IOpenAiOptions>();
                ArgumentException.ThrowIfNullOrWhiteSpace(openAiOptions.EmbeddingModel);

                return TiktokenTokenizer.CreateForModel(openAiOptions.EmbeddingModel);
            });

            services.TryAddSingleton<Lazy<IEmbeddingGenerator<string, Embedding<float>>>>(provider =>
                new Lazy<IEmbeddingGenerator<string, Embedding<float>>>(provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>));
            services.TryAddSingleton<Lazy<Tokenizer>>(provider =>
                new Lazy<Tokenizer>(provider.GetRequiredService<Tokenizer>));

            services.TryAddScoped<IEmbeddingService, EmbeddingService>();

            return services;
        }
    }
}
