using PublicSafetyHub.Integration.IntegrationModels;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface IArmyArticleImportService : IArticleImportService
{
    Task<IReadOnlyList<ArmyArticle>> GetArticlesAsync();
}
