using AutoMapper;
using FluentValidation;
using Pgvector;
using PublicSafetyHub.Core.Constants;
using PublicSafetyHub.AI.Services.Interfaces;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Core.Specifications;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Application.Projections;
using PublicSafetyHub.Models.Articles;
using PublicSafetyHub.Base.Filtering;

namespace PublicSafetyHub.Application.Services;

public class ArticleService(
    IArticleRepository articleRepository,
    IMapper mapper,
    IValidator<PaginationRequest> paginationRequestValidator,
    IEmbeddingService embeddingService,
    IOpenAiOptions openAiOptions) : IArticleService
{
    public async Task<ApplicationResult<Article>> GetArticleByIdAsync(int id)
    {
        var article = await articleRepository.GetAsync(id);

        if (article is null)
        {
            return ResultHelper.NotFound("Article was not found.");
        }

        return mapper.Map<Article>(article);
    }

    public async Task<ApplicationResult<PagedResult<Article>>> GetArticlesAsync(FilteredRequest<ArticleFilter> filteredRequest)
    {
        var validationResult = await paginationRequestValidator.ValidateAsync(filteredRequest);

        if (!validationResult.IsValid)
        {
            return ResultHelper.Validation(validationResult.Errors[0].ErrorMessage);
        }

        var specification = filteredRequest.Filter is null ? new ArticleSpecification() : mapper.Map<ArticleSpecification>(filteredRequest.Filter);

        if (specification.HasSearch && specification.SemanticSearch)
        {
            if (string.IsNullOrWhiteSpace(openAiOptions.ApiKey) || string.IsNullOrWhiteSpace(openAiOptions.EmbeddingModel))
            {
                return ResultHelper.Unavailable("Semantic search is not configured.");
            }

            var searchValue = specification.SearchValue.Trim();

            if (embeddingService.GetTokenCount(searchValue) > EmbeddingConstants.MaxInputTokens)
            {
                return ResultHelper.Validation($"SearchValue must not exceed {EmbeddingConstants.MaxInputTokens} tokens for semantic search.");
            }

            var vector = await embeddingService.GenerateAsync(searchValue);

            if (vector.All(value => value == 0))
            {
                return ResultHelper.Unavailable("Semantic search could not generate a usable query embedding.");
            }

            specification.QueryEmbedding = new Vector(vector);
        }

        return specification.HasSearch
            ? await articleRepository.SearchArticlesAsync(specification, filteredRequest, ArticleProjection.Article, article => article.Id)
            : await articleRepository.GetPagedAsync(specification, filteredRequest, projection: ArticleProjection.Article);
    }
}
