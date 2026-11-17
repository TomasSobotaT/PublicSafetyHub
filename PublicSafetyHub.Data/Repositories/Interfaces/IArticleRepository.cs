using PublicSafetyHub.Base.Repositories;
using PublicSafetyHub.Core.Entities;

namespace PublicSafetyHub.Data.Repositories.Interfaces;

public interface IArticleRepository : IExtendedBaseRepository<ArticleEntity, int>
{
    Task<bool> ExistsByOriginUrlAsync(string originUrl);
}
