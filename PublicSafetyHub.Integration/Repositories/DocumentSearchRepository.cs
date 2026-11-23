using OpenSearch.Client;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Integration.IntegrationModels;
using PublicSafetyHub.Integration.Repositories.Interfaces;

namespace PublicSafetyHub.Integration.Repositories;

public class DocumentSearchRepository(IOpenSearchClient client, IOpenSearchOptions options) : IDocumentSearchRepository
{
    public async Task IndexAsync(SearchDocument document)
    {
        await EnsureIndexAsync();

        var response = await client.IndexAsync(document, request => request
            .Index(options.DocumentsIndex)
            .Id(document.Id));

        if (!response.IsValid)
        {
            throw new InvalidOperationException("OpenSearch document indexing failed.");
        }
    }

    public async Task<IReadOnlyList<SearchDocument>> SearchAsync(string searchText, int limit)
    {
        var response = await client.SearchAsync<SearchDocument>(request => request
            .Index(options.DocumentsIndex)
            .AllowPartialSearchResults(false)
            .Size(limit)
            .Query(query => query.MultiMatch(match => match
                .Query(searchText)
                .Fields(fields => fields
                    .Field(document => document.Title, 2)
                    .Field(document => document.Body)))));

        if (response.ApiCall?.HttpStatusCode == 404 && response.ServerError?.Error?.Type == "index_not_found_exception")
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
        var existsResponse = await client.Indices.ExistsAsync(options.DocumentsIndex);

        if (!existsResponse.IsValid)
        {
            throw new InvalidOperationException("OpenSearch document index could not be checked.");
        }

        if (existsResponse.Exists)
        {
            return;
        }

        var response = await client.Indices.CreateAsync(options.DocumentsIndex, request => request
            .Map<SearchDocument>(mapping => mapping
                .Dynamic(DynamicMapping.Strict)
                .Properties(properties => properties
                    .Keyword(field => field.Name(document => document.Id))
                    .Text(field => field.Name(document => document.Title).Analyzer("czech"))
                    .Text(field => field.Name(document => document.Body).Analyzer("czech"))
                    .Keyword(field => field.Name(document => document.Url).Index(false)))));

        if (!response.IsValid && response.ServerError?.Error?.Type != "resource_already_exists_exception")
        {
            throw new InvalidOperationException("OpenSearch document index could not be created.");
        }

        if (response.IsValid && !response.Acknowledged)
        {
            throw new InvalidOperationException("OpenSearch document index creation was not acknowledged.");
        }
    }
}
