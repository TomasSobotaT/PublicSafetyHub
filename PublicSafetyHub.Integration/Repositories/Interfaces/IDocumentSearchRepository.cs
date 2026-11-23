using PublicSafetyHub.Integration.IntegrationModels;
using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Integration.Repositories.Interfaces;

public interface IDocumentSearchRepository
{
    Task IndexAsync(SearchDocument document);

    Task<IReadOnlyList<SearchDocument>> SearchAsync(string searchText, int limit, DocumentSearchMode searchMode, float[] queryEmbedding = null);
}
