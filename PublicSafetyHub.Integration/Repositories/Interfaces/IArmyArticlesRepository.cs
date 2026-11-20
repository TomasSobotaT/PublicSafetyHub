using PublicSafetyHub.Integration.IntegrationModels;

namespace PublicSafetyHub.Integration.Repositories.Interfaces;

public interface IArmyArticlesRepository
{
    Task<IReadOnlyList<ArmyArticle>> GetArticlesAsync();
}

