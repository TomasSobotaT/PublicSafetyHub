using PublicSafetyHub.Integration.IntegrationModels;

namespace PublicSafetyHub.Integration.Repositories.Interfaces;

public interface IPoliceArticlesRepository
{
    Task<IReadOnlyList<PoliceArticleLink>> GetListAsync(int pageNumber);

    Task<PoliceArticle> GetArticleAsync(string originUrl);
}

