namespace PublicSafetyHub.Core.Configurations;

public interface IPostgreSqlLoggingOptions
{
    string ConnectionStringName { get; }

    int RetentionDays { get; }

    int BatchSizeLimit { get; }

    int QueueLimit { get; }

    int PeriodSeconds { get; }
}
