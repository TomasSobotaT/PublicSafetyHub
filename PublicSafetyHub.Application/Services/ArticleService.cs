using AutoMapper;
using FluentValidation;
using Pgvector;
using PublicSafetyHub.AI.Services;
using PublicSafetyHub.AI.Services.Interfaces;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Pagination;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Core.Specifications;
using PublicSafetyHub.Core.Repositories;
using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Application.Services;

public class ArticleService(
    IArticleRepository articleRepository,
    IMapper mapper,
    IValidator<PaginationRequest> paginationRequestValidator,
    IEmbeddingService embeddingService) : IArticleService
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
            var searchValue = specification.SearchValue.Trim();

            if (embeddingService.GetTokenCount(searchValue) > EmbeddingService.MaxInputTokens)
            {
                return ResultHelper.Validation($"SearchValue must not exceed {EmbeddingService.MaxInputTokens} tokens for semantic search.");
            }

            var vector = await embeddingService.GenerateAsync(searchValue);

            if (vector.All(value => value == 0))
            {
                return ResultHelper.Unavailable("Semantic search could not generate a usable query embedding.");
            }

            specification.QueryEmbedding = new Vector(vector);
        }

        var articles = await articleRepository.GetPagedAsync(specification, filteredRequest);

        return mapper.Map<PagedResult<Article>>(articles);
    }
}
