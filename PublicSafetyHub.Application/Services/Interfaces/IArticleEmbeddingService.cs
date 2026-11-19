namespace PublicSafetyHub.Application.Services.Interfaces;

public interface IArticleEmbeddingService
{
    Task GenerateMissingEmbeddingsAsync();
}
