using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Base.Repositories;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Models.Enums;
using PublicSafetyHub.Data.Context;
using PublicSafetyHub.Core.Repositories;

namespace PublicSafetyHub.Data.Repositories;

public class ArticleRepository(PublicSafetyHubDbContext dbContext) : ExtendedBaseRepository<ArticleEntity, int>(dbContext), IArticleRepository
{
    public Task<List<ArticleEntity>> GetArtíclesWithoutEmbeddingAsync(int afterId, int count)
    {
        return GetQueryable()
            .Where(article => article.Id > afterId && article.Embedding == null)
            .OrderBy(article => article.Id)
            .Take(count)
            .ToListAsync();
    }

    public Task<DateTime?> GetLatestPublishedAtAsync(ArticleType type)
    {
        return GetQueryable()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(article => article.Type == type && article.PublishedAt <= DateTime.UtcNow)
            .MaxAsync(article => article.PublishedAt);
    }

    public async Task<List<string>> GetExistingOriginUrlsAsync(IReadOnlyCollection<string> originUrls)
    {
        if (originUrls.Count == 0)
        {
            return [];
        }

        return await GetQueryable()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(article => originUrls.Contains(article.OriginUrl))
            .Select(article => article.OriginUrl)
            .ToListAsync();
    }
}
