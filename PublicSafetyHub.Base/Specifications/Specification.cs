using System.Linq.Expressions;
using PublicSafetyHub.Base.Filtering;

namespace PublicSafetyHub.Base.Specifications;

public abstract class Specification<TEntity> : ISpecification<TEntity> where TEntity : class
{
    protected abstract IReadOnlyDictionary<string, Expression<Func<TEntity, object>>> SortColumns { get; }

    protected abstract Expression<Func<TEntity, object>> IdSelector { get; }

    protected virtual Expression<Func<TEntity, object>> DefaultOrderBy => IdSelector;

    protected virtual Expression<Func<TEntity, object>> DefaultThenBy => null;

    protected virtual bool DefaultSortDescending => false;

    public Expression<Func<TEntity, object>> OrderBy { get; private set; }

    public Expression<Func<TEntity, object>> ThenBy { get; private set; }

    public bool SortDescending { get; private set; }

    public int? PageNumber { get; private set; }

    public int? PageSize { get; private set; }

    public abstract IEnumerable<Expression<Func<TEntity, bool>>> GetCriteria();

    public void ApplyPagination(PaginationRequest paginationRequest)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(paginationRequest.PageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(paginationRequest.PageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(paginationRequest.PageSize, 100);

        var sortBy = string.IsNullOrWhiteSpace(paginationRequest.SortBy) ? "" : paginationRequest.SortBy.Trim();

        if (!SortColumns.TryGetValue(sortBy, out var orderBy))
        {
            OrderBy = DefaultOrderBy;
            ThenBy = DefaultThenBy;
            SortDescending = DefaultSortDescending;
        }
        else
        {
            OrderBy = orderBy;
            ThenBy = string.Equals(sortBy, "Id", StringComparison.OrdinalIgnoreCase) ? null : IdSelector;
            SortDescending = string.Equals(paginationRequest.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        }

        PageNumber = paginationRequest.PageNumber;
        PageSize = paginationRequest.PageSize;
    }
}
