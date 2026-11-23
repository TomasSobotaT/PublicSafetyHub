using PublicSafetyHub.Integration.IntegrationModels;

namespace PublicSafetyHub.Integration.Repositories.Interfaces;

public interface IDocumentSearchRepository
{
    Task IndexAsync(SearchDocument document);

    Task<IReadOnlyList<SearchDocument>> SearchAsync(string searchText, int limit);
}
