using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Core.Repositories.Integration;

public interface IFirefightersArticlesRepository
{
    Task<IReadOnlyList<string>> GetArticleLinksFromEmailNewslettersAsync(CancellationToken cancellationToken);

    Task<FirefightersArticle> GetArticleAsync(string originUrl, CancellationToken cancellationToken);
}
