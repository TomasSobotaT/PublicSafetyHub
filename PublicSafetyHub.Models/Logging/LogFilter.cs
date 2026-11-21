using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Models.Logging;

public class LogFilter
{
    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public LogSeverity[] Levels { get; set; }

    public string Category { get; set; }

    public string SearchValue { get; set; }

    public string TraceId { get; set; }
}
