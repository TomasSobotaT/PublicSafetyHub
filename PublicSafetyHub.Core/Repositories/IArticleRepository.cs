using PublicSafetyHub.Base.Repositories;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Enums;
using PublicSafetyHub.Core.Specifications;
using System.Linq.Expressions;
using PublicSafetyHub.Base.Filtering;

namespace PublicSafetyHub.Core.Repositories;

public interface IArticleRepository : IExtendedBaseRepository<ArticleEntity, int>
{
    Task<PagedResult<TResult>> SearchArticlesAsync<TResult>(ArticleSpecification specification, PaginationRequest paginationRequest,
        Expression<Func<ArticleEntity, TResult>> projection, Func<TResult, int> idSelector);

    Task<List<string>> GetExistingOriginUrlsAsync(IReadOnlyCollection<string> originUrls);

    Task<DateTime?> GetLatestPublishedAtAsync(ArticleType type);

    Task<List<ArticleEntity>> GetArtíclesWithoutEmbeddingAsync(int lastCheckedArticleId, int count);
}
