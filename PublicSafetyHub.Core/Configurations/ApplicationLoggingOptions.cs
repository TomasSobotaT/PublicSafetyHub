namespace PublicSafetyHub.Core.Configurations;

public class ApplicationLoggingOptions : IApplicationLoggingOptions
{
    public string FilePath { get; set; } = "Logs/application-.json";

    public int RetainedFileCountLimit { get; set; } = 30;

    public long FileSizeLimitBytes { get; set; } = 10 * 1024 * 1024;
}
