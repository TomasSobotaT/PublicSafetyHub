using OpenSearch.Client;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Integration.IntegrationModels;
using PublicSafetyHub.Integration.Repositories.Interfaces;
using PublicSafetyHub.Core.Constants;
using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Integration.Repositories;

public class DocumentSearchRepository(IOpenSearchClient openSearchClient, IOpenSearchOptions openSearchOptions) : IDocumentSearchRepository
{
    private const string ResourceAlreadyExistsErrorType = "resource_already_exists_exception";
    private const string IndexNotFoundErrorType = "index_not_found_exception";
    private const double TitleSearchBoost = 2;
    private const int SearchCandidateLimit = 100;
    private const int ReciprocalRankConstant = 60;
    private const string EmbeddingFieldName = "embedding";

    public async Task IndexAsync(SearchDocument document)
    {
        ValidateEmbedding(document.Embedding);
        await EnsureIndexAsync();

        var response = await openSearchClient.IndexAsync(document, request => request
            .Index(openSearchOptions.DocumentsIndex)
            .Id(document.Id));

        if (!response.IsValid)
        {
            throw new InvalidOperationException("OpenSearch document indexing failed.");
        }
    }

    public async Task<IReadOnlyList<SearchDocument>> SearchAsync(string searchText, int limit, DocumentSearchMode searchMode, float[] queryEmbedding = null)
    {
        if (searchMode == DocumentSearchMode.FullText)
        {
            return await SearchFullTextAsync(searchText, limit);
        }

        if (searchMode != DocumentSearchMode.Semantic && searchMode != DocumentSearchMode.Hybrid)
        {
            throw new ArgumentOutOfRangeException(nameof(searchMode), searchMode, "Unknown document search mode.");
        }

        ValidateEmbedding(queryEmbedding);

        if (searchMode == DocumentSearchMode.Semantic)
        {
            return await SearchSemanticAsync(queryEmbedding, limit);
        }

        var fullTextDocuments = await SearchFullTextAsync(searchText, SearchCandidateLimit);
        var semanticDocuments = await SearchSemanticAsync(queryEmbedding, SearchCandidateLimit);

        return [.. fullTextDocuments
            .Select((document, index) => new { Document = document, Score = 1.0 / (ReciprocalRankConstant + index + 1) })
            .Concat(semanticDocuments.Select((document, index) => new { Document = document, Score = 1.0 / (ReciprocalRankConstant + index + 1) }))
            .GroupBy(result => result.Document.Id)
            .OrderByDescending(group => group.Sum(result => result.Score))
            .ThenBy(group => group.Key)
            .Take(limit)
            .Select(group => group.First().Document)];
    }

    private Task<IReadOnlyList<SearchDocument>> SearchFullTextAsync(string searchText, int limit)
    {
        var query = new QueryContainerDescriptor<SearchDocument>().MultiMatch(match => match
            .Query(searchText)
            .Fields(fields => fields
                .Field(document => document.Title, TitleSearchBoost)
                .Field(document => document.Body)));

        return ExecuteSearchAsync(query, limit);
    }

    private Task<IReadOnlyList<SearchDocument>> SearchSemanticAsync(float[] queryEmbedding, int limit)
    {
        var query = new QueryContainerDescriptor<SearchDocument>().ScriptScore(score => score
            .Query(filter => filter.Exists(exists => exists.Field(document => document.Embedding)))
            .Script(script => script
                .Source("knn_score")
                .Lang("knn")
                .Params(parameters => parameters
                    .Add("field", EmbeddingFieldName)
                    .Add("query_value", queryEmbedding)
                    .Add("space_type", "cosinesimil"))));

        return ExecuteSearchAsync(query, limit);
    }

    private async Task<IReadOnlyList<SearchDocument>> ExecuteSearchAsync(QueryContainer query, int limit)
    {
        var response = await openSearchClient.SearchAsync<SearchDocument>(request => request
            .Index(openSearchOptions.DocumentsIndex)
            .AllowPartialSearchResults(false)
            .Source(source => source.Excludes(fields => fields.Field(document => document.Embedding)))
            .Size(limit)
            .Sort(sort => sort.Descending(SortSpecialField.Score).Ascending(document => document.Id))
            .Query(_ => query));

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
            await EnsureEmbeddingMappingAsync();
            return;
        }

        var response = await openSearchClient.Indices.CreateAsync(openSearchOptions.DocumentsIndex, request => request
            .Map<SearchDocument>(mapping => mapping
                .Dynamic(DynamicMapping.Strict)
                .Properties(properties => properties
                    .Keyword(field => field.Name(document => document.Id))
                    .Text(field => field.Name(document => document.Title).Analyzer("czech"))
                    .Text(field => field.Name(document => document.Body).Analyzer("czech"))
                    .Keyword(field => field.Name(document => document.Url).Index(false))
                    .KnnVector(field => field.Name(EmbeddingFieldName).Dimension(EmbeddingConstants.Dimensions)))));

        if (!response.IsValid && response.ServerError?.Error?.Type != ResourceAlreadyExistsErrorType)
        {
            throw new InvalidOperationException("OpenSearch document index could not be created.");
        }

        if (response.IsValid && !response.Acknowledged)
        {
            throw new InvalidOperationException("OpenSearch document index creation was not acknowledged.");
        }

        if (!response.IsValid)
        {
            await EnsureEmbeddingMappingAsync();
        }
    }

    private async Task EnsureEmbeddingMappingAsync()
    {
        var response = await openSearchClient.Indices.PutMappingAsync<SearchDocument>(mapping => mapping
            .Index(openSearchOptions.DocumentsIndex)
            .Properties(properties => properties
                .KnnVector(field => field.Name(EmbeddingFieldName).Dimension(EmbeddingConstants.Dimensions))));

        if (!response.IsValid || !response.Acknowledged)
        {
            throw new InvalidOperationException("OpenSearch document embedding mapping could not be configured. The field must be knn_vector with the configured embedding dimensions.");
        }
    }

    private static void ValidateEmbedding(float[] embedding)
    {
        if (embedding is null || embedding.Length != EmbeddingConstants.Dimensions || embedding.Any(value => !float.IsFinite(value)) || embedding.All(value => value == 0))
        {
            throw new ArgumentException($"Embedding must contain {EmbeddingConstants.Dimensions} finite values and must not be a zero vector.", nameof(embedding));
        }
    }
}
