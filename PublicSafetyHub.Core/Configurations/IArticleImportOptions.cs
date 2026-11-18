namespace PublicSafetyHub.Core.Configurations;

public interface IArticleImportOptions
{
    TimeOnly MorningTime { get; }

    TimeOnly EveningTime { get; }

    string TimeZoneId { get; }
}
