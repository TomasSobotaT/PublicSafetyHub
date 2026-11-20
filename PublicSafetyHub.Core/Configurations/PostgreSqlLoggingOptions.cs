namespace PublicSafetyHub.Core.Configurations;

public class PostgreSqlLoggingOptions : IPostgreSqlLoggingOptions
{
    public string ConnectionStringName { get; set; } = "DefaultConnection";

    public int RetentionDays { get; set; } = 30;

    public int BatchSizeLimit { get; set; } = 50;

    public int QueueLimit { get; set; } = 10000;

    public int PeriodSeconds { get; set; } = 5;
}
