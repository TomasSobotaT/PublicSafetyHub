using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Models.Documents;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface IDocumentService
{
    Task<ApplicationResult<Guid>> SaveDocumentAsync(Document document);

    Task<ApplicationResult<IReadOnlyList<Document>>> SearchDocumentsAsync(string searchText, int limit = 5);
}
