using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Models.Logging;

public class LogFilter
{
    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public LogSeverity? Level { get; set; }

    public string Category { get; set; }

    public string SearchValue { get; set; }

    public string TraceId { get; set; }
}
