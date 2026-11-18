namespace PublicSafetyHub.AI.Services.Interfaces;

public interface IEmbeddingService
{
    int CountTokens(string text);

    Task<IReadOnlyDictionary<int, float[]>> GenerateAsync(IReadOnlyDictionary<int, string> textToEmbeddingDictionary);
}
