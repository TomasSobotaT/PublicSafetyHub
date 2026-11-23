using AutoMapper;
using FluentValidation;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Integration.IntegrationModels;
using PublicSafetyHub.Integration.Repositories.Interfaces;
using PublicSafetyHub.Models.Documents;
using PublicSafetyHub.Core.Constants;
using PublicSafetyHub.AI.Services.Interfaces;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Application.Services;

public class DocumentService(
    IDocumentSearchRepository documentRepository,
    IValidator<Document> documentValidator,
    IMapper mapper,
    IEmbeddingService embeddingService,
    IOpenAiOptions openAiOptions) : IDocumentService
{
    public async Task<ApplicationResult<Guid>> SaveDocumentAsync(Document document)
    {
        if (document is null)
        {
            return ResultHelper.Validation("Document is required.");
        }

        var validation = await documentValidator.ValidateAsync(document);

        if (!validation.IsValid)
        {
            return ResultHelper.Validation(validation.Errors[0].ErrorMessage);
        }

        if (!IsEmbeddingConfigured())
        {
            return ResultHelper.Unavailable("Document embedding generation is not configured.");
        }

        var embeddingInput = $"Title: {document.Title.Trim()}\n\n{document.Body.Trim()}";

        if (embeddingService.GetTokenCount(embeddingInput) > EmbeddingConstants.MaxInputTokens)
        {
            return ResultHelper.Validation($"Document embedding input must not exceed {EmbeddingConstants.MaxInputTokens} tokens.");
        }

        var embedding = await embeddingService.GenerateAsync(embeddingInput);

        if (embedding.All(value => value == 0))
        {
            return ResultHelper.Unavailable("Document embedding generation returned an unusable vector.");
        }

        var searchDocument = mapper.Map<SearchDocument>(document);
        searchDocument.Embedding = embedding;

        if (searchDocument.Id == Guid.Empty)
        {
            searchDocument.Id = Guid.CreateVersion7();
        }

        await documentRepository.IndexAsync(searchDocument);

        return searchDocument.Id;
    }

    public async Task<ApplicationResult<IReadOnlyList<Document>>> SearchDocumentsAsync(string searchText, int limit = 5, SearchMode searchMode = SearchMode.FullText)
    {
        if (string.IsNullOrWhiteSpace(searchText) || searchText.Length > 2048)
        {
            return ResultHelper.Validation("Search text is required and must not exceed 2048 characters.");
        }

        if (limit < 1 || limit > 100)
        {
            return ResultHelper.Validation("Search limit must be between 1 and 100.");
        }

        var normalizedSearchText = searchText.Trim();
        float[] queryEmbedding = null;

        if (searchMode != SearchMode.FullText)
        {
            if (!IsEmbeddingConfigured())
            {
                return ResultHelper.Unavailable("Semantic document search is not configured.");
            }

            if (embeddingService.GetTokenCount(normalizedSearchText) > EmbeddingConstants.MaxInputTokens)
            {
                return ResultHelper.Validation($"Search text must not exceed {EmbeddingConstants.MaxInputTokens} tokens for semantic search.");
            }

            queryEmbedding = await embeddingService.GenerateAsync(normalizedSearchText);

            if (queryEmbedding.All(value => value == 0))
            {
                return ResultHelper.Unavailable("Semantic document search could not generate a usable query embedding.");
            }
        }

        var documents = await documentRepository.SearchAsync(normalizedSearchText, limit, searchMode, queryEmbedding);
        return mapper.Map<List<Document>>(documents);
    }

    private bool IsEmbeddingConfigured()
    {
        return !string.IsNullOrWhiteSpace(openAiOptions.ApiKey) &&
            openAiOptions.ApiKey != "REPLACE_WITH_OPENAI_API_KEY" &&
            !string.IsNullOrWhiteSpace(openAiOptions.EmbeddingModel);
    }
}
