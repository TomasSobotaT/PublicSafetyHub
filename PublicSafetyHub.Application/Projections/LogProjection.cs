using System.Linq.Expressions;
using PublicSafetyHub.Core.Entities;
using PublicSafetyHub.Core.Enums;
using PublicSafetyHub.Models.Logging;

namespace PublicSafetyHub.Application.Projections;

public static class LogProjection
{
    public static readonly Expression<Func<LogEntity, LogSummary>> Summary = log => new LogSummary
    {
        Id = log.Id,
        Timestamp = log.Timestamp,
        Level = (LogSeverity)log.Level,
        Category = log.Category,
        Message = log.Message,
        TraceId = log.TraceId,
        HasException = log.Exception != null && log.Exception != ""
    };

    public static readonly Expression<Func<LogEntity, Log>> Detail = log => new Log
    {
        Id = log.Id,
        Timestamp = log.Timestamp,
        Level = (LogSeverity)log.Level,
        Category = log.Category,
        Message = log.Message,
        TraceId = log.TraceId,
        HasException = log.Exception != null && log.Exception != "",
        MessageTemplate = log.MessageTemplate,
        Exception = log.Exception,
        Properties = log.Properties
    };
}
