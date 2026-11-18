namespace PublicSafetyHub.Core.Configurations;

public interface IPoliceArticleImportOptions
{
    string BaseUrl { get; }

    string ListPath { get; }

    bool Enabled { get; }
}
