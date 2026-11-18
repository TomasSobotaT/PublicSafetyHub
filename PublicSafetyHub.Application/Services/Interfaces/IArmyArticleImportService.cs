using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface IArmyArticleImportService
{
    Task ImportFeedAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ArmyArticle>> GetArticlesAsync();
}
