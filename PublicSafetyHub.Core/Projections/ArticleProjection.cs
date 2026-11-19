using System.Linq.Expressions;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Models.Articles;

namespace PublicSafetyHub.Core.Projections;

public static class ArticleProjection
{
    public static readonly Expression<Func<ArticleEntity, Article>> Project = article => new Article
    {
        Id = article.Id,
        Title = article.Title,
        Content = article.Content,
        OriginUrl = article.OriginUrl,
        Type = article.Type,
        CreatedAt = article.CreatedAt,
        PublishedAt = article.PublishedAt,
        UpdatedAt = article.UpdatedAt
    };
}
