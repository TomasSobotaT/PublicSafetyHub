using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface IArmyArticleImportService : IArticleImportService
{
    Task<IReadOnlyList<ArmyArticle>> GetArticlesAsync();
}
