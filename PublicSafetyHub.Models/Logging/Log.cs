namespace PublicSafetyHub.Models.Logging;

public class Log : LogSummary
{
    public string MessageTemplate { get; set; }

    public string Exception { get; set; }

    public string Properties { get; set; }
}
