namespace PublicSafetyHub.Base.Specifications;

public static class SpecificationEvaluator
{
    public static IQueryable<TEntity> GetQuery<TEntity>(IQueryable<TEntity> query, ISpecification<TEntity> specification, bool criteriaOnly = false) where TEntity : class
    {
        if (specification is null)
        {
            return query;
        }

        foreach (var predicate in specification.GetCriteria())
        {
            query = query.Where(predicate);
        }

        if (criteriaOnly)
        {
            return query;
        }

        if (specification.OrderBy is not null)
        {
            var orderedQuery = specification.SortDescending
                ? query.OrderByDescending(specification.OrderBy)
                : query.OrderBy(specification.OrderBy);
            query = specification.ThenBy is null ? orderedQuery : orderedQuery.ThenBy(specification.ThenBy);
        }

        if (specification.PageNumber.HasValue && specification.PageSize.HasValue)
        {
            var offset = checked((specification.PageNumber.Value - 1) * specification.PageSize.Value);
            query = query.Skip(offset).Take(specification.PageSize.Value);
        }

        return query;
    }
}
