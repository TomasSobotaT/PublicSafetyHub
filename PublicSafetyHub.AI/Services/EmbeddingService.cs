using Microsoft.Extensions.AI;
using Microsoft.ML.Tokenizers;
using PublicSafetyHub.AI.Services.Interfaces;
using PublicSafetyHub.Core.Constants;

namespace PublicSafetyHub.AI.Services;

public class EmbeddingService(Lazy<IEmbeddingGenerator<string, Embedding<float>>> embeddingGenerator, Lazy<Tokenizer> tokenizer) : IEmbeddingService
{
    public int GetTokenCount(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return tokenizer.Value.CountTokens(text);
    }

    public async Task<float[]> GenerateAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var embedding = await embeddingGenerator.Value.GenerateVectorAsync(text,
            new EmbeddingGenerationOptions { Dimensions = EmbeddingConstants.Dimensions });
        var vector = embedding.ToArray();

        if (IsInvalidVector(vector))
        {
            throw new InvalidOperationException("The embedding provider returned an invalid vector.");
        }

        return vector;
    }

    public async Task<IReadOnlyDictionary<int, float[]>> GenerateAsync(IReadOnlyDictionary<int, string> textToEmbeddingDictionary)
    {
        ArgumentNullException.ThrowIfNull(textToEmbeddingDictionary);

        if (textToEmbeddingDictionary.Count == 0)
        {
            return new Dictionary<int, float[]>();
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(textToEmbeddingDictionary.Count, EmbeddingConstants.MaxBatchSize);

        var inputKeyValuePairs = textToEmbeddingDictionary.ToArray();
        var uniqueTexts = inputKeyValuePairs.Select(input => input.Value).Distinct(StringComparer.Ordinal).ToArray();

        var embeddings = await embeddingGenerator.Value.GenerateAndZipAsync(uniqueTexts, new EmbeddingGenerationOptions { Dimensions = EmbeddingConstants.Dimensions });
        var vectorsByTextDictionary = new Dictionary<string, float[]>(StringComparer.Ordinal);

        foreach (var (text, embedding) in embeddings)
        {
            var vector = embedding.Vector.ToArray();

            if (IsInvalidVector(vector))
            {
                throw new InvalidOperationException("The embedding provider returned an invalid vector.");
            }

            vectorsByTextDictionary.Add(text, vector);
        }

        return inputKeyValuePairs.ToDictionary(input => input.Key, input => vectorsByTextDictionary[input.Value]);
    }

    private static bool IsInvalidVector(float[] vector)
    {
        return vector.Length != EmbeddingConstants.Dimensions || vector.Any(value => !float.IsFinite(value));
    }
}
