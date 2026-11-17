using System.Linq.Expressions;
using PublicSafetyHub.Base.Specifications;
using PublicSafetyHub.Core.Entities;

namespace PublicSafetyHub.Core.Specifications;

public class ArticleSpecification : Specification<ArticleEntity>
{
    public string Type { get; set; }

    public string SearchValue { get; set; }

    protected override Expression<Func<ArticleEntity, object>> IdSelector => article => article.Id;

    protected override IReadOnlyDictionary<string, Expression<Func<ArticleEntity, object>>> SortColumns => AllowedSorts;

    private static readonly IReadOnlyDictionary<string, Expression<Func<ArticleEntity, object>>> AllowedSorts =
        new Dictionary<string, Expression<Func<ArticleEntity, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(ArticleEntity.Id)] = article => article.Id,
            [nameof(ArticleEntity.Title)] = article => article.Title,
            [nameof(ArticleEntity.Type)] = article => article.Type,
            [nameof(ArticleEntity.CreatedAt)] = article => article.CreatedAt,
            [nameof(ArticleEntity.UpdatedAt)] = article => article.UpdatedAt
        };

    public override IEnumerable<Expression<Func<ArticleEntity, bool>>> GetCriteria()
    {
        var criteria = new List<Expression<Func<ArticleEntity, bool>>>();

        if (!string.IsNullOrWhiteSpace(Type))
        {
            var type = Type.Trim().ToLowerInvariant();
            criteria.Add(article => article.Type.ToString().ToLower() == type);
        }

        if (!string.IsNullOrWhiteSpace(SearchValue))
        {
            var searchValue = SearchValue.Trim().ToLowerInvariant();
            criteria.Add(article => article.Title.ToLower().Contains(searchValue) || article.Content.ToLower().Contains(searchValue));
        }

        return criteria;
    }
}
