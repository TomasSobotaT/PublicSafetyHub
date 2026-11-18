namespace PublicSafetyHub.Core.Configurations;

public class ArticleImportOptions : IArticleImportOptions
{
    public TimeOnly MorningTime { get; set; }

    public TimeOnly EveningTime { get; set; }

    public string TimeZoneId { get; set; }
}
