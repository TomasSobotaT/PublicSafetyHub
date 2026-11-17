using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Filtering;
using PublicSafetyHub.Base.Pagination;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Models.Articles.Filters;
using PublicSafetyHub.Models.Articles.Responses;

namespace PublicSafetyHub.Host.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/articles")]
[AllowAnonymous]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
public class ArticlesController(IArticleService articleService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType<Article>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArticleByIdAsync(int id)
    {
        var result = await articleService.GetArticleByIdAsync(id);

        return result.ToActionResult();
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<Article>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetArticlesAsync([FromQuery] FilteredRequest<ArticleFilter> filteredRequest)
    {
        var result = await articleService.GetArticlesAsync(filteredRequest);

        return result.ToActionResult();
    }
}
