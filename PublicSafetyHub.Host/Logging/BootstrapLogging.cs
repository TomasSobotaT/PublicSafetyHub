using Serilog;
using Serilog.Debugging;
using Serilog.Formatting.Compact;

namespace PublicSafetyHub.Host.Logging;

internal static class BootstrapLogging
{
    public static void Initialize()
    {
        SelfLog.Enable(TextWriter.Synchronized(Console.Error));
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.Console()
            .WriteTo.File(new CompactJsonFormatter(), "Logs/bootstrap-.json",
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 30,
                shared: true)
            .CreateBootstrapLogger();
    }
}
