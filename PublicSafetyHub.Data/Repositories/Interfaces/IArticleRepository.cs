using PublicSafetyHub.Base.Repositories;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Data.Repositories.Interfaces;

public interface IArticleRepository : IExtendedBaseRepository<ArticleEntity, int>
{
    Task<List<string>> GetExistingOriginUrlsAsync(IReadOnlyCollection<string> originUrls);

    Task<DateTime?> GetLatestPublishedAtAsync(ArticleType type);
}
