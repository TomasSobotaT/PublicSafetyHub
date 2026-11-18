using PublicSafetyHub.Base.Repositories;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Core.Repositories;

public interface IArticleRepository : IExtendedBaseRepository<ArticleEntity, int>
{
    Task<List<string>> GetExistingOriginUrlsAsync(IReadOnlyCollection<string> originUrls);

    Task<DateTime?> GetLatestPublishedAtAsync(ArticleType type);
}
