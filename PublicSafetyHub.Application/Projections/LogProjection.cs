using System.Linq.Expressions;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Models.Logging;

namespace PublicSafetyHub.Application.Projections;

public static class LogProjection
{
    public static readonly Expression<Func<LogEntity, Log>> Log = log => new Log
    {
        Id = log.Id,
        Timestamp = log.Timestamp,
        Level = log.Level,
        Category = log.Category,
        Message = log.Message,
        TraceId = log.TraceId,
        HasException = !string.IsNullOrWhiteSpace(log.Exception),
        Exception = log.Exception,
        Properties = log.Properties
    };
}
