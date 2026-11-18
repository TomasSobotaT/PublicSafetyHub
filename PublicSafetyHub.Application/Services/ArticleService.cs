using AutoMapper;
using FluentValidation;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Pagination;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Core.Specifications;
using PublicSafetyHub.Data.Repositories.Interfaces;
using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Application.Services;

public class ArticleService(IArticleRepository articleRepository, IMapper mapper, IValidator<PaginationRequest> paginationRequestValidator) : IArticleService
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
        var articles = await articleRepository.GetPagedAsync(specification, filteredRequest);

        return mapper.Map<PagedResult<Article>>(articles);
    }
}
