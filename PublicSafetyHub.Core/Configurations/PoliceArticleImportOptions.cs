namespace PublicSafetyHub.Core.Configurations;

public class PoliceArticleImportOptions : IPoliceArticleImportOptions
{
    public string BaseUrl { get; set; }

    public string ListPath { get; set; }

    public bool Enabled { get; set; }
}
