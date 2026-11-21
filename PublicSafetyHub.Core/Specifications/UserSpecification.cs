using System.Linq.Expressions;
using PublicSafetyHub.Base.Specifications;
using PublicSafetyHub.Core.Entities;

namespace PublicSafetyHub.Core.Specifications;

public class UserSpecification : Specification<UserEntity>
{
    public string SearchValue { get; set; }

    public bool? IsBlocked { get; set; }

    protected override Expression<Func<UserEntity, object>> IdSelector => user => user.Id;

    protected override IReadOnlyDictionary<string, Expression<Func<UserEntity, object>>> SortColumns => AllowedSorts;

    private static readonly IReadOnlyDictionary<string, Expression<Func<UserEntity, object>>> AllowedSorts =
        new Dictionary<string, Expression<Func<UserEntity, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(UserEntity.Id)] = user => user.Id,
            [nameof(UserEntity.Email)] = user => user.Email,
            [nameof(UserEntity.CreatedAt)] = user => user.CreatedAt,
            [nameof(UserEntity.UpdatedAt)] = user => user.UpdatedAt,
            [nameof(UserEntity.IsBlocked)] = user => user.IsBlocked
        };

    public override IEnumerable<Expression<Func<UserEntity, bool>>> GetCriteria()
    {
        var criteria = new List<Expression<Func<UserEntity, bool>>>();

        if (!string.IsNullOrWhiteSpace(SearchValue))
        {
            var normalizedSearch = SearchValue.Trim().ToUpperInvariant();
            criteria.Add(user => user.NormalizedEmail.Contains(normalizedSearch));
        }

        if (IsBlocked.HasValue)
        {
            criteria.Add(user => user.IsBlocked == IsBlocked.Value);
        }

        return criteria;
    }
}
