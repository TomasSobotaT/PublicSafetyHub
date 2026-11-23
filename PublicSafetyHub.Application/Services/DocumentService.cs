using AutoMapper;
using FluentValidation;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Integration.IntegrationModels;
using PublicSafetyHub.Integration.Repositories.Interfaces;
using PublicSafetyHub.Models.Documents;

namespace PublicSafetyHub.Application.Services;

public class DocumentService(
    IDocumentSearchRepository repository,
    IValidator<Document> validator,
    IMapper mapper) : IDocumentService
{
    public async Task<ApplicationResult<Guid>> SaveDocumentAsync(Document document)
    {
        if (document is null)
        {
            return ResultHelper.Validation("Document is required.");
        }

        var validation = await validator.ValidateAsync(document);

        if (!validation.IsValid)
        {
            return ResultHelper.Validation(validation.Errors[0].ErrorMessage);
        }

        var searchDocument = mapper.Map<SearchDocument>(document);

        if (searchDocument.Id == Guid.Empty)
        {
            searchDocument.Id = Guid.CreateVersion7();
        }

        await repository.IndexAsync(searchDocument);

        return searchDocument.Id;
    }

    public async Task<ApplicationResult<IReadOnlyList<Document>>> SearchDocumentsAsync(string searchText, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(searchText) || searchText.Length > 2048)
        {
            return ResultHelper.Validation("Search text is required and must not exceed 2048 characters.");
        }

        if (limit < 1 || limit > 100)
        {
            return ResultHelper.Validation("Search limit must be between 1 and 100.");
        }

        var documents = await repository.SearchAsync(searchText.Trim(), limit);
        return mapper.Map<List<Document>>(documents);
    }
}
