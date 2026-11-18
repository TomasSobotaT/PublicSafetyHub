namespace PublicSafetyHub.Core.Configurations;

public class ArmyArticleImportOptions : IArmyArticleImportOptions
{
    public string FeedUrl { get; set; }

    public string BaseUrl { get; set; }

    public bool Enabled { get; set; }
}
