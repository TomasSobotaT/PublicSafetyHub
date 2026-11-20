using PublicSafetyHub.Integration.IntegrationModels;

namespace PublicSafetyHub.Integration.Repositories.Interfaces;

public interface IFirefightersArticlesRepository
{
    Task<IReadOnlyList<string>> GetArticleLinksFromEmailNewslettersAsync();

    Task<FirefightersArticle> GetArticleAsync(string originUrl);
}
