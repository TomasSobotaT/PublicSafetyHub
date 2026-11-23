using OpenSearch.Client;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Integration.IntegrationModels;
using PublicSafetyHub.Integration.Repositories.Interfaces;

namespace PublicSafetyHub.Integration.Repositories;

public class DocumentSearchRepository(IOpenSearchClient openSearchClient, IOpenSearchOptions openSearchOptions) : IDocumentSearchRepository
{
    private const string ResourceAlreadyExistsErrorType = "resource_already_exists_exception";
    private const string IndexNotFoundErrorType = "index_not_found_exception";
    private const double TitleSearchBoost = 2;

    public async Task IndexAsync(SearchDocument document)
    {
        await EnsureIndexAsync();

        var response = await openSearchClient.IndexAsync(document, request => request
            .Index(openSearchOptions.DocumentsIndex)
            .Id(document.Id));

        if (!response.IsValid)
        {
            throw new InvalidOperationException("OpenSearch document indexing failed.");
        }
    }

    public async Task<IReadOnlyList<SearchDocument>> SearchAsync(string searchText, int limit)
    {
        var response = await openSearchClient.SearchAsync<SearchDocument>(request => request
            .Index(openSearchOptions.DocumentsIndex)
            .AllowPartialSearchResults(false)
            .Size(limit)
            .Query(query => query.MultiMatch(match => match
                .Query(searchText)
                .Fields(fields => fields
                    .Field(document => document.Title, TitleSearchBoost)
                    .Field(document => document.Body)))));

        if (response.ApiCall?.HttpStatusCode == 404 && response.ServerError?.Error?.Type == IndexNotFoundErrorType)
        {
            return [];
        }

        if (!response.IsValid || response.TimedOut || response.Shards?.Failed > 0)
        {
            throw new InvalidOperationException("OpenSearch document search failed or returned incomplete results.");
        }

        return [.. response.Documents];
    }

    private async Task EnsureIndexAsync()
    {
        var existsResponse = await openSearchClient.Indices.ExistsAsync(openSearchOptions.DocumentsIndex);

        if (!existsResponse.IsValid)
        {
            throw new InvalidOperationException("OpenSearch document index could not be checked.");
        }

        if (existsResponse.Exists)
        {
            return;
        }

        var response = await openSearchClient.Indices.CreateAsync(openSearchOptions.DocumentsIndex, request => request
            .Map<SearchDocument>(mapping => mapping
                .Dynamic(DynamicMapping.Strict)
                .Properties(properties => properties
                    .Keyword(field => field.Name(document => document.Id))
                    .Text(field => field.Name(document => document.Title).Analyzer("czech"))
                    .Text(field => field.Name(document => document.Body).Analyzer("czech"))
                    .Keyword(field => field.Name(document => document.Url).Index(false)))));

        if (!response.IsValid && response.ServerError?.Error?.Type != ResourceAlreadyExistsErrorType)
        {
            throw new InvalidOperationException("OpenSearch document index could not be created.");
        }

        if (response.IsValid && !response.Acknowledged)
        {
            throw new InvalidOperationException("OpenSearch document index creation was not acknowledged.");
        }
    }
}
