using PublicSafetyHub.Base.Extensions.Filtering;
using PublicSafetyHub.Base.Pagination;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface IArticleService
{
    Task<ApplicationResult<Article>> GetArticleByIdAsync(int id);

    Task<ApplicationResult<PagedResult<Article>>> GetArticlesAsync(FilteredRequest<ArticleFilter> filteredRequest);
}
