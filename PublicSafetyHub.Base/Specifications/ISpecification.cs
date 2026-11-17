using System.Linq.Expressions;

namespace PublicSafetyHub.Base.Specifications;

public interface ISpecification<TEntity> where TEntity : class
{
    IEnumerable<Expression<Func<TEntity, bool>>> GetCriteria();

    Expression<Func<TEntity, object>> OrderBy { get; }

    Expression<Func<TEntity, object>> ThenBy { get; }

    bool SortDescending { get; }

    int? PageNumber { get; }

    int? PageSize { get; }
}
