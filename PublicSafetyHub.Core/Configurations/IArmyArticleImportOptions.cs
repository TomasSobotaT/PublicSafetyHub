namespace PublicSafetyHub.Core.Configurations;

public interface IArmyArticleImportOptions
{
    string FeedUrl { get; }

    string BaseUrl { get; }

    bool Enabled { get; }

    TimeOnly MorningTime { get; }

    TimeOnly EveningTime { get; }

    string TimeZoneId { get; }
}
