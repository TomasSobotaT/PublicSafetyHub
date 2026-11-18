using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Core.Repositories.Integration;

public interface IPoliceArticlesRepository
{
    Task<IReadOnlyList<PoliceArticleLink>> GetListAsync(int pageNumber);

    Task<PoliceArticle> GetArticleAsync(string originUrl);
}

