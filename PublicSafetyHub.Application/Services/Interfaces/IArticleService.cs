using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Pagination;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Models.Articles.Filters;
using PublicSafetyHub.Models.Articles.Responses;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface IArticleService
{
    Task<ApplicationResult<Article>> GetArticleByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ApplicationResult<PagedResult<Article>>> GetArticlesAsync(FilteredRequest<ArticleFilter> filteredRequest, CancellationToken cancellationToken = default);
}
