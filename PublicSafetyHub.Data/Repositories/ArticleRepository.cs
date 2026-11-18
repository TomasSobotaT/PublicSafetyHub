using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Base.Repositories;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Data.Context;
using PublicSafetyHub.Data.Repositories.Interfaces;

namespace PublicSafetyHub.Data.Repositories;

public class ArticleRepository(PublicSafetyHubDbContext dbContext) : ExtendedBaseRepository<ArticleEntity, int>(dbContext), IArticleRepository
{
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
