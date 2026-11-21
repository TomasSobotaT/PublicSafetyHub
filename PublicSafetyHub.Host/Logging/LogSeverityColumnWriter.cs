using NpgsqlTypes;
using PublicSafetyHub.Core.Enums;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace PublicSafetyHub.Host.Logging;

public sealed class LogSeverityColumnWriter() : ColumnWriterBase(NpgsqlDbType.Text)
{
    public override object GetValue(LogEvent logEvent, IFormatProvider formatProvider = null)
    {
        return logEvent.Level switch
        {
            LogEventLevel.Verbose => nameof(LogSeverity.Trace),
            LogEventLevel.Debug => nameof(LogSeverity.Debug),
            LogEventLevel.Information => nameof(LogSeverity.Information),
            LogEventLevel.Warning => nameof(LogSeverity.Warning),
            LogEventLevel.Error => nameof(LogSeverity.Error),
            LogEventLevel.Fatal => nameof(LogSeverity.Critical),
            _ => throw new ArgumentOutOfRangeException(nameof(logEvent))
        };
    }
}
