using NpgsqlTypes;
using PublicSafetyHub.Base.Extensions;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Data.Configurations;
using PublicSafetyHub.Host.Logging;
using Serilog;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class LoggingExtension
{
    extension(IHostApplicationBuilder builder)
    {
        public void AddPostgreSqlLogging()
        {
            builder.Services.AddConfiguration<IPostgreSqlLoggingOptions, PostgreSqlLoggingOptions>(builder.Configuration, postgreSqlLoggingOptions =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(postgreSqlLoggingOptions.ConnectionStringName);
                ArgumentOutOfRangeException.ThrowIfLessThan(postgreSqlLoggingOptions.RetentionDays, 1);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(postgreSqlLoggingOptions.RetentionDays, 36500);
                ArgumentOutOfRangeException.ThrowIfLessThan(postgreSqlLoggingOptions.BatchSizeLimit, 1);
                ArgumentOutOfRangeException.ThrowIfLessThan(postgreSqlLoggingOptions.QueueLimit, postgreSqlLoggingOptions.BatchSizeLimit);
                ArgumentOutOfRangeException.ThrowIfLessThan(postgreSqlLoggingOptions.PeriodSeconds, 1);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(postgreSqlLoggingOptions.PeriodSeconds, 3600);
                ArgumentException.ThrowIfNullOrWhiteSpace(builder.Configuration.GetConnectionString(postgreSqlLoggingOptions.ConnectionStringName));
            });
            builder.Services.AddSerilog((services, logger) =>
            {
                var postgreSqlLoggingOptions = services.GetRequiredService<IPostgreSqlLoggingOptions>();
                var connectionString = builder.Configuration.GetConnectionString(postgreSqlLoggingOptions.ConnectionStringName);

                logger.ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.With<TraceContextEnricher>();

                logger.WriteTo.PostgreSQL(
                    connectionString,
                    LogConfiguration.TableName,
                    new Dictionary<string, ColumnWriterBase>
                    {
                        ["Timestamp"] = new TimestampColumnWriter(),
                        ["Id"] = new LogIdColumnWriter(),
                        ["Level"] = new LogSeverityColumnWriter(),
                        ["Category"] = new SinglePropertyColumnWriter("SourceContext", PropertyWriteMethod.Raw, NpgsqlDbType.Text),
                        ["Message"] = new RenderedMessageColumnWriter(),
                        ["Exception"] = new ExceptionColumnWriter(),
                        ["TraceId"] = new SinglePropertyColumnWriter("TraceId", PropertyWriteMethod.Raw, NpgsqlDbType.Text),
                        ["Properties"] = new PropertiesColumnWriter()
                    },
                    schemaName: LogConfiguration.SchemaName,
                    needAutoCreateTable: false,
                    needAutoCreateSchema: false,
                    batchSizeLimit: postgreSqlLoggingOptions.BatchSizeLimit,
                    queueLimit: postgreSqlLoggingOptions.QueueLimit,
                    period: TimeSpan.FromSeconds(postgreSqlLoggingOptions.PeriodSeconds),
                    retentionTime: TimeSpan.FromDays(postgreSqlLoggingOptions.RetentionDays));
            });
        }
    }
}
