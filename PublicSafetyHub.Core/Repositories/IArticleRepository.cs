using PublicSafetyHub.Base.Repositories;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Models.Enums;
using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Pagination;
using PublicSafetyHub.Core.Specifications;
using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Core.Repositories;

public interface IArticleRepository : IExtendedBaseRepository<ArticleEntity, int>
{
    Task<PagedResult<Article>> GetArticlesAsync(ArticleSpecification specification, PaginationRequest paginationRequest);

    Task<List<string>> GetExistingOriginUrlsAsync(IReadOnlyCollection<string> originUrls);

    Task<DateTime?> GetLatestPublishedAtAsync(ArticleType type);

    Task<List<ArticleEntity>> GetArtíclesWithoutEmbeddingAsync(int lastCheckedArticleId, int count);
}
