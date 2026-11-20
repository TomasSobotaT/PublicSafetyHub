namespace PublicSafetyHub.Core.Configurations;

public interface IApplicationLoggingOptions
{
    string FilePath { get; }

    int RetainedFileCountLimit { get; }

    long FileSizeLimitBytes { get; }
}
