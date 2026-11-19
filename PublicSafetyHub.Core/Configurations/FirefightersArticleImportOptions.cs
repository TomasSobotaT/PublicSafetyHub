namespace PublicSafetyHub.Core.Configurations;

public class FirefightersArticleImportOptions : IFirefightersArticleImportOptions
{
    public bool Enabled { get; set; }
    public string ImapHost { get; set; }
    public int ImapPort { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}
