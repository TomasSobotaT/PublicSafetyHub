using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Models.Logging;

public class Log
{
    public Guid Id { get; set; }

    public DateTime Timestamp { get; set; }

    public LogSeverity Level { get; set; }

    public string Category { get; set; }

    public string Message { get; set; }

    public string TraceId { get; set; }

    public bool HasException { get; set; }

    public string Exception { get; set; }

    public string Properties { get; set; }
}
