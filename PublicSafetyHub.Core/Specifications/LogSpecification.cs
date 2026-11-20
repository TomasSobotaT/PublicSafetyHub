using System.Linq.Expressions;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Base.Specifications;
using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Core.Specifications;

public class LogSpecification : Specification<LogEntity>
{
    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public LogSeverity? Level { get; set; }

    public string Category { get; set; }

    public string SearchValue { get; set; }

    public string TraceId { get; set; }

    protected override Expression<Func<LogEntity, object>> IdSelector => log => log.Id;

    protected override Expression<Func<LogEntity, object>> DefaultOrderBy => log => log.Timestamp;

    protected override Expression<Func<LogEntity, object>> DefaultThenBy => IdSelector;

    protected override bool DefaultSortDescending => true;

    protected override IReadOnlyDictionary<string, Expression<Func<LogEntity, object>>> SortColumns => AllowedSorts;

    private static readonly IReadOnlyDictionary<string, Expression<Func<LogEntity, object>>> AllowedSorts =
        new Dictionary<string, Expression<Func<LogEntity, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(LogEntity.Id)] = log => log.Id,
            [nameof(LogEntity.Timestamp)] = log => log.Timestamp,
            [nameof(LogEntity.Level)] = log => log.Level,
            [nameof(LogEntity.Category)] = log => log.Category
        };

    public override IEnumerable<Expression<Func<LogEntity, bool>>> GetCriteria()
    {
        var criteria = new List<Expression<Func<LogEntity, bool>>>();

        if (From.HasValue)
        {
            criteria.Add(log => log.Timestamp >= From.Value);
        }

        if (To.HasValue)
        {
            criteria.Add(log => log.Timestamp <= To.Value);
        }

        if (Level.HasValue)
        {
            var level = (int)Level.Value;
            criteria.Add(log => log.Level == level);
        }

        if (!string.IsNullOrWhiteSpace(Category))
        {
            criteria.Add(log => log.Category != null && log.Category.Contains(Category));
        }

        if (!string.IsNullOrWhiteSpace(SearchValue))
        {
            criteria.Add(log => log.Message.Contains(SearchValue));
        }

        if (!string.IsNullOrWhiteSpace(TraceId))
        {
            criteria.Add(log => log.TraceId == TraceId);
        }

        return criteria;
    }
}
