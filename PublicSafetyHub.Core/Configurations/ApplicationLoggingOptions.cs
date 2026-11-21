namespace PublicSafetyHub.Core.Configurations;

public class ApplicationLoggingOptions : IApplicationLoggingOptions
{
    public string FilePath { get; set; }

    public int RetainedFileCountLimit { get; set; }

    public long FileSizeLimitBytes { get; set; }
}
