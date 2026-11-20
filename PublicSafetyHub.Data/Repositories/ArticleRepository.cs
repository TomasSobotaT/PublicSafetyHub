using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PublicSafetyHub.Base.Repositories;
using PublicSafetyHub.Base.Specifications;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Enums;

using PublicSafetyHub.Data.Context;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Core.Specifications;
using NpgsqlTypes;
using Pgvector.EntityFrameworkCore;
using PublicSafetyHub.Base.Filtering;

namespace PublicSafetyHub.Data.Repositories;

public class ArticleRepository(PublicSafetyHubDbContext dbContext) : ExtendedBaseRepository<ArticleEntity, int>(dbContext), IArticleRepository
{
    private const int ReciprocalRankConstant = 60;
    private const int SearchCandidateLimit = 100;

    public async Task<PagedResult<TResult>> SearchArticlesAsync<TResult>(ArticleSpecification specification, PaginationRequest paginationRequest, Expression<Func<ArticleEntity, TResult>> projection, Func<TResult, int> idSelector)
    {
        ArgumentNullException.ThrowIfNull(specification);
        ArgumentNullException.ThrowIfNull(paginationRequest);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(idSelector);

        if (!specification.HasSearch)
        {
            throw new ArgumentException("Article search requires a search value and an enabled search mode.", nameof(specification));
        }

        specification.ApplyPagination(paginationRequest);
        var query = GetQueryable().AsNoTracking();
        var filteredArticles = SpecificationEvaluator.GetQuery(query, specification, criteriaOnly: true);
        var searchValue = specification.SearchValue.Trim();
        List<int> fullTextArticleIds = [];
        List<int> semanticArticleIds = [];


        if (specification.FullTextSearch)
        {
            var matches = filteredArticles
                .Where(article => EF.Property<NpgsqlTsVector>(article, "SearchVector")
                    .Matches(EF.Functions.WebSearchToTsQuery("simple", searchValue)));

            var rankedMatches = matches
                .OrderByDescending(article => EF.Property<NpgsqlTsVector>(article, "SearchVector")
                    .RankCoverDensity(EF.Functions.WebSearchToTsQuery("simple", searchValue)))
                .ThenBy(article => article.Id);

            if (!specification.SemanticSearch)
            {
                var totalCount = await matches.CountAsync();
                var offset = checked((specification.PageNumber.Value - 1) * specification.PageSize.Value);
                var pageQuery = ArticleSpecification.IsAllowedSort(paginationRequest.SortBy)
                    ? SpecificationEvaluator.GetQuery(matches, specification)
                    : rankedMatches.Skip(offset).Take(specification.PageSize.Value);
                var items = await pageQuery.Select(projection).ToListAsync();

                return new PagedResult<TResult>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = specification.PageNumber.Value,
                    PageSize = specification.PageSize.Value
                };
            }

            fullTextArticleIds = await rankedMatches
                .Select(article => article.Id)
                .Take(SearchCandidateLimit)
                .ToListAsync();
        }

        if (specification.SemanticSearch)
        {
            var queryEmbedding = specification.QueryEmbedding ?? throw new InvalidOperationException("Semantic search requires a query embedding.");

            var semanticMatches = filteredArticles.Where(article => article.Embedding != null);

            var rankedSemanticMatches = semanticMatches
                .OrderBy(article => article.Embedding.CosineDistance(queryEmbedding))
                .ThenBy(article => article.Id);

            semanticArticleIds = await rankedSemanticMatches
                .Select(article => article.Id)
                .Take(SearchCandidateLimit)
                .ToListAsync();
        }

        var rankedArticleIds = semanticArticleIds;
        if (specification.FullTextSearch && specification.SemanticSearch)
        {
            rankedArticleIds = [.. fullTextArticleIds
                .Select((id, index) => new { Id = id, Score = 1.0 / (ReciprocalRankConstant + index + 1) })
                .Concat(semanticArticleIds.Select((id, index) => new { Id = id, Score = 1.0 / (ReciprocalRankConstant + index + 1) }))
                .GroupBy(result => result.Id)
                .OrderByDescending(group => group.Sum(result => result.Score))
                .ThenBy(group => group.Key)
                .Select(group => group.Key)];
        }

        List<TResult> articles;

        if (ArticleSpecification.IsAllowedSort(paginationRequest.SortBy))
        {
            var matchingArticles = filteredArticles.Where(article => rankedArticleIds.Contains(article.Id));
            articles = await SpecificationEvaluator.GetQuery(matchingArticles, specification).Select(projection).ToListAsync();
        }
        else
        {
            var offset = checked((specification.PageNumber.Value - 1) * specification.PageSize.Value);
            var pageArticleIds = rankedArticleIds.Skip(offset).Take(specification.PageSize.Value).ToList();
            var pageArticles = await filteredArticles.Where(article => pageArticleIds.Contains(article.Id)).Select(projection).ToDictionaryAsync(idSelector);
            articles = [.. pageArticleIds.Where(pageArticles.ContainsKey).Select(id => pageArticles[id])];
        }

        return new PagedResult<TResult>
        {
            Items = articles,
            TotalCount = rankedArticleIds.Count,
            PageNumber = specification.PageNumber.Value,
            PageSize = specification.PageSize.Value
        };
    }

    public Task<List<ArticleEntity>> GetArtíclesWithoutEmbeddingAsync(int lastCheckedArticleId, int count)
    {
        return GetQueryable()
            .Where(article => article.Id > lastCheckedArticleId && article.Embedding == null)
            .OrderBy(article => article.Id)
            .Take(count)
            .ToListAsync();
    }

    public Task<DateTime?> GetLatestPublishedAtAsync(ArticleType type)
    {
        return GetQueryable()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(article => article.Type == type && article.PublishedAt <= DateTime.UtcNow)
            .MaxAsync(article => article.PublishedAt);
    }

    public async Task<List<string>> GetExistingOriginUrlsAsync(IReadOnlyCollection<string> originUrls)
    {
        if (originUrls.Count == 0)
        {
            return [];
        }

        return await GetQueryable()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(article => originUrls.Contains(article.OriginUrl))
            .Select(article => article.OriginUrl)
            .ToListAsync();
    }
}
