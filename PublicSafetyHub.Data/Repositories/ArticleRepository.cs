using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Base.Repositories;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Data.Context;
using PublicSafetyHub.Data.Repositories.Interfaces;

namespace PublicSafetyHub.Data.Repositories;

public class ArticleRepository(PublicSafetyHubDbContext dbContext) : ExtendedBaseRepository<ArticleEntity, int>(dbContext), IArticleRepository
{
    public Task<bool> ExistsByOriginUrlAsync(string originUrl)
    {
        return GetQueryable().IgnoreQueryFilters().AnyAsync(article => article.OriginUrl == originUrl);
    }
}
