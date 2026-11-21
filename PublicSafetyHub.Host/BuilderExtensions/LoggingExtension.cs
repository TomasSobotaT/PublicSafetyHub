using NpgsqlTypes;
using PublicSafetyHub.Base.Extensions;
using PublicSafetyHub.Core.Configurations;
using PublicSafetyHub.Data.Configurations;
using PublicSafetyHub.Host.Logging;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace PublicSafetyHub.Host.BuilderExtensions;

public static class LoggingExtension
{
    extension(IHostApplicationBuilder builder)
    {
        public void AddApplicationLogging(Action<IServiceProvider, LoggerConfiguration> configureOutput = null)
        {
            builder.Services.AddConfiguration<IApplicationLoggingOptions, ApplicationLoggingOptions>(builder.Configuration, options =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(options.FilePath);
                ArgumentOutOfRangeException.ThrowIfLessThan(options.RetainedFileCountLimit, 1);
                ArgumentOutOfRangeException.ThrowIfLessThan(options.FileSizeLimitBytes, 1);
            });

            builder.Services.AddSerilog((services, logger) =>
            {
                var options = services.GetRequiredService<IApplicationLoggingOptions>();

                logger.ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.With<TraceContextEnricher>()
                    .WriteTo.Console()
                    .WriteTo.File(new CompactJsonFormatter(), options.FilePath,
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: options.FileSizeLimitBytes,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: options.RetainedFileCountLimit,
                        shared: true);

                configureOutput?.Invoke(services, logger);
            });
        }

        public void AddPostgreSqlLogging()
        {
            builder.Services.AddConfiguration<IPostgreSqlLoggingOptions, PostgreSqlLoggingOptions>(builder.Configuration, options =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(options.ConnectionStringName);
                ArgumentOutOfRangeException.ThrowIfLessThan(options.RetentionDays, 1);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(options.RetentionDays, 36500);
                ArgumentOutOfRangeException.ThrowIfLessThan(options.BatchSizeLimit, 1);
                ArgumentOutOfRangeException.ThrowIfLessThan(options.QueueLimit, options.BatchSizeLimit);
                ArgumentOutOfRangeException.ThrowIfLessThan(options.PeriodSeconds, 1);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(options.PeriodSeconds, 3600);
                ArgumentException.ThrowIfNullOrWhiteSpace(builder.Configuration.GetConnectionString(options.ConnectionStringName));
            });
            builder.AddApplicationLogging((services, logger) =>
            {
                var options = services.GetRequiredService<IPostgreSqlLoggingOptions>();
                var connectionString = builder.Configuration.GetConnectionString(options.ConnectionStringName);

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
                    batchSizeLimit: options.BatchSizeLimit,
                    queueLimit: options.QueueLimit,
                    period: TimeSpan.FromSeconds(options.PeriodSeconds),
                    retentionTime: TimeSpan.FromDays(options.RetentionDays));
            });
        }
    }
}
