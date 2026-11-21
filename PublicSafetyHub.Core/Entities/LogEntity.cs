using PublicSafetyHub.Base.Entities.Base;
using PublicSafetyHub.Core.Enums;

namespace PublicSafetyHub.Core.Entities;

public class LogEntity : BaseEntity<Guid>
{
    public DateTime Timestamp { get; set; }

    public LogSeverity Level { get; set; }

    public string Category { get; set; }

    public string Message { get; set; }

    public string Exception { get; set; }

    public string TraceId { get; set; }

    public string Properties { get; set; }
}
