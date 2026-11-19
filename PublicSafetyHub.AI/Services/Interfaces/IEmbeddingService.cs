namespace PublicSafetyHub.AI.Services.Interfaces;

public interface IEmbeddingService
{
    int GetTokenCount(string text);

    Task<float[]> GenerateAsync(string text);

    Task<IReadOnlyDictionary<int, float[]>> GenerateAsync(IReadOnlyDictionary<int, string> textToEmbeddingDictionary);
}
