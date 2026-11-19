using PublicSafetyHub.Base.Repositories;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Models.Enums;

namespace PublicSafetyHub.Core.Repositories;

public interface IArticleRepository : IExtendedBaseRepository<ArticleEntity, int>
{
    Task<List<string>> GetExistingOriginUrlsAsync(IReadOnlyCollection<string> originUrls);

    Task<List<string>> GetExistingOriginUrlsAsync(IReadOnlyCollection<string> originUrls, CancellationToken cancellationToken);

    Task<DateTime?> GetLatestPublishedAtAsync(ArticleType type);

    Task<List<ArticleEntity>> GetArtíclesWithoutEmbeddingAsync(int lastCheckedArticleId, int count);
}
