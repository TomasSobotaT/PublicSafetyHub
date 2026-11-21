using NpgsqlTypes;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace PublicSafetyHub.Host.Logging;

public sealed class LogIdColumnWriter() : ColumnWriterBase(NpgsqlDbType.Uuid)
{
    public override object GetValue(LogEvent logEvent, IFormatProvider formatProvider = null)
    {
        return Guid.CreateVersion7(logEvent.Timestamp);
    }
}
