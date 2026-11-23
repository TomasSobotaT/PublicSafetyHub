using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Models.Documents;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface IDocumentService
{
    Task<ApplicationResult<NoValueResult>> UpsertAsync(Document document);

    Task<ApplicationResult<IReadOnlyList<Document>>> SearchAsync(string searchText, int limit = 5);
}
