namespace PublicSafetyHub.Core.Configurations;

public class ArmyArticleImportOptions : IArmyArticleImportOptions
{
    public string FeedUrl { get; set; }

    public string BaseUrl { get; set; }

    public bool Enabled { get; set; }

    public TimeOnly MorningTime { get; set; }

    public TimeOnly EveningTime { get; set; }

    public string TimeZoneId { get; set; }
}
