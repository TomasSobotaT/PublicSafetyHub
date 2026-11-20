namespace PublicSafetyHub.Core.Entities;

public class LogEntity
{
    public long Id { get; set; }

    public DateTime Timestamp { get; set; }

    public int Level { get; set; }

    public string Category { get; set; }

    public string MessageTemplate { get; set; }

    public string Message { get; set; }

    public string Exception { get; set; }

    public string TraceId { get; set; }

    public string Properties { get; set; }
}
