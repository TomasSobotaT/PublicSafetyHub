namespace PublicSafetyHub.Core.Configurations;

public class PostgreSqlLoggingOptions : IPostgreSqlLoggingOptions
{
    public string ConnectionStringName { get; set; }

    public int RetentionDays { get; set; }

    public int BatchSizeLimit { get; set; }

    public int QueueLimit { get; set; }

    public int PeriodSeconds { get; set; }
}
