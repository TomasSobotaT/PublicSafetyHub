using Microsoft.Extensions.AI;
using Microsoft.ML.Tokenizers;
using PublicSafetyHub.AI.Services.Interfaces;

namespace PublicSafetyHub.AI.Services;

public class EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, Tokenizer tokenizer) : IEmbeddingService
{
    public const int Dimensions = 1536;
    public const int MaxInputTokens = 8191;
    public const int MaxBatchSize = 32;

    public int GetTokenCount(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return tokenizer.CountTokens(text);
    }

    public async Task<IReadOnlyDictionary<int, float[]>> GenerateAsync(IReadOnlyDictionary<int, string> textToEmbeddingDictionary)
    {
        ArgumentNullException.ThrowIfNull(textToEmbeddingDictionary);

        if (textToEmbeddingDictionary.Count == 0)
        {
            return new Dictionary<int, float[]>();
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(textToEmbeddingDictionary.Count, MaxBatchSize);

        var inputKeyValuePairs = textToEmbeddingDictionary.ToArray();
        var uniqueTexts = inputKeyValuePairs.Select(input => input.Value).Distinct(StringComparer.Ordinal).ToArray();

        var embeddings = await embeddingGenerator.GenerateAndZipAsync(uniqueTexts, new EmbeddingGenerationOptions { Dimensions = Dimensions });
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
        return vector.Length != Dimensions || vector.Any(value => !float.IsFinite(value));
    }
}
