using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Core.Repositories.Integration;

public interface IArmyArticlesRepository
{
    Task<IReadOnlyList<ArmyArticle>> GetArticlesAsync();
}

