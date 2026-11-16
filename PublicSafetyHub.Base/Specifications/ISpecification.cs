using System.Linq.Expressions;

namespace PublicSafetyHub.Base.Specifications;

public interface ISpecification<TEntity> where TEntity : class
{
    Expression<Func<TEntity, bool>> Criteria { get; }

    List<Expression<Func<TEntity, object>>> Includes { get; }

    Expression<Func<TEntity, object>> OrderBy { get; }

    bool OrderDescending { get; }
}
