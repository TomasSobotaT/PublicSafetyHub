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
    public async Task<ApplicationResult<Article>> GetArticleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var article = await articleRepository.GetAsync(id, cancellationToken);

        if (article is null)
        {
            return ResultHelper.NotFound("Article was not found.");
        }

        return mapper.Map<Article>(article);
    }

    public async Task<ApplicationResult<PagedResult<Article>>> GetArticlesAsync(FilteredRequest<ArticleFilter> filteredRequest, CancellationToken cancellationToken = default)
    {
        var specification = filteredRequest.Filter is null ? new ArticleSpecification() : mapper.Map<ArticleSpecification>(filteredRequest.Filter);

        var articleFilter = filteredRequest.Filter;

        if (articleFilter is not null && !ArticleSpecification.IsTypeSupported(articleFilter.Type))
        {
            return ResultHelper.Validation("Article type must be Police, Firefighters or Army.");
        }

        specification.ApplyPagination(filteredRequest);
        var articles = await articleRepository.GetPagedAsync(specification, query => mapper.ProjectTo<Article>(query), cancellationToken);

        return articles;
    }
}
