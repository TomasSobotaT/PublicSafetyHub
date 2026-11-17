using AutoMapper;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Pagination;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Data.Repositories.Interfaces;
using PublicSafetyHub.Data.Specifications;
using PublicSafetyHub.Models.Articles.Filters;
using PublicSafetyHub.Models.Articles.Responses;

namespace PublicSafetyHub.Application.Services;

public class ArticleService(IArticleRepository articleRepository, IMapper mapper) : IArticleService
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
        var specification = filteredRequest.Filter is null ? new ArticleSpecification() : mapper.Map<ArticleSpecification>(filteredRequest.Filter);
        var articles = await articleRepository.GetPagedAsync(specification, filteredRequest);

        return mapper.Map<PagedResult<Article>>(articles);
    }
}
