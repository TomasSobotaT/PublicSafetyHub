namespace PublicSafetyHub.Core.Configurations;

public interface IArmyArticleImportOptions
{
    string FeedUrl { get; }

    string BaseUrl { get; }

    bool Enabled { get; }
}
