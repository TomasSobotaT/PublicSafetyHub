using Microsoft.Extensions.AI;
using Microsoft.ML.Tokenizers;
using PublicSafetyHub.AI.Services.Interfaces;

namespace PublicSafetyHub.AI.Services;

public class EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> generator) : IEmbeddingService
{
    public const int Dimensions = 1536;
    public const int MaxInputTokens = 8191;
    public const int MaxBatchSize = 32;

    private static readonly Tokenizer tokenizer = TiktokenTokenizer.CreateForModel("text-embedding-3-small");

    public int CountTokens(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return tokenizer.CountTokens(text);
    }

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateAsync(IReadOnlyList<string> texts)
    {
        ArgumentNullException.ThrowIfNull(texts);

        if (texts.Count == 0)
        {
            return [];
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(texts.Count, MaxBatchSize);

        foreach (var text in texts)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);

            if (CountTokens(text) > MaxInputTokens)
            {
                throw new ArgumentException("Embedding input exceeds the token limit.", nameof(texts));
            }
        }

        // Each input is a separate article, not a concatenation of the whole batch.
        var embeddings = await generator.GenerateAsync(texts, new EmbeddingGenerationOptions { Dimensions = Dimensions });

        if (embeddings.Count != texts.Count)
        {
            throw new InvalidOperationException("The embedding provider returned an unexpected number of vectors.");
        }

        var vectors = new List<ReadOnlyMemory<float>>(embeddings.Count);

        foreach (var embedding in embeddings)
        {
            var vector = embedding.Vector;

            if (vector.Length != Dimensions || vector.ToArray().Any(value => !float.IsFinite(value)))
            {
                throw new InvalidOperationException("The embedding provider returned an invalid vector.");
            }

            vectors.Add(vector);
        }

        return vectors;
    }
}
