using System.Linq.Expressions;
using Pgvector;
using PublicSafetyHub.Base.Specifications;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Core.Specifications;

public class ArticleSpecification : Specification<ArticleEntity>
{
    public ArticleType? Type { get; set; }

    public string SearchValue { get; set; }

    public SearchMode? SearchMode { get; set; }

    public Vector QueryEmbedding { get; set; }

    public bool HasSearch => !string.IsNullOrWhiteSpace(SearchValue) && SearchMode.HasValue;

    public static bool IsAllowedSort(string sortBy) => !string.IsNullOrWhiteSpace(sortBy) && AllowedSorts.ContainsKey(sortBy.Trim());

    protected override Expression<Func<ArticleEntity, object>> IdSelector => article => article.Id;

    protected override Expression<Func<ArticleEntity, object>> DefaultOrderBy => article => article.PublishedAt ?? article.CreatedAt;

    protected override Expression<Func<ArticleEntity, object>> DefaultThenBy => IdSelector;

    protected override bool DefaultSortDescending => true;

    protected override IReadOnlyDictionary<string, Expression<Func<ArticleEntity, object>>> SortColumns => AllowedSorts;

    private static readonly IReadOnlyDictionary<string, Expression<Func<ArticleEntity, object>>> AllowedSorts =
        new Dictionary<string, Expression<Func<ArticleEntity, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(ArticleEntity.Id)] = article => article.Id,
            [nameof(ArticleEntity.Title)] = article => article.Title,
            [nameof(ArticleEntity.Type)] = article => article.Type,
            [nameof(ArticleEntity.CreatedAt)] = article => article.CreatedAt,
            [nameof(ArticleEntity.PublishedAt)] = article => article.PublishedAt,
            [nameof(ArticleEntity.UpdatedAt)] = article => article.UpdatedAt
        };

    public override IEnumerable<Expression<Func<ArticleEntity, bool>>> GetCriteria()
    {
        var criteria = new List<Expression<Func<ArticleEntity, bool>>>();

        if (Type.HasValue)
        {
            var type = Type.Value;
            criteria.Add(article => article.Type == type);
        }

        return criteria;
    }
}
