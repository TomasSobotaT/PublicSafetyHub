namespace PublicSafetyHub.AI.Services.Interfaces;

public interface IEmbeddingService
{
    int CountTokens(string text);

    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateAsync(IReadOnlyList<string> texts);
}
