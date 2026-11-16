using System.Linq.Expressions;

namespace PublicSafetyHub.Base.Specifications;

public class Specification<TEntity> : ISpecification<TEntity> where TEntity : class
{
    public Expression<Func<TEntity, bool>> Criteria { get; set; }

    public List<Expression<Func<TEntity, object>>> Includes { get; } = [];

    public Expression<Func<TEntity, object>> OrderBy { get; set; }

    public bool OrderDescending { get; set; }
}
