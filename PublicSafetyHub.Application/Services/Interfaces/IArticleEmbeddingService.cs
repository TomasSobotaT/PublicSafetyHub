namespace PublicSafetyHub.Application.Services.Interfaces;

public interface IArticleEmbeddingService
{
    Task<int?> GenerateBatchAsync(int afterId, CancellationToken cancellationToken);
}
