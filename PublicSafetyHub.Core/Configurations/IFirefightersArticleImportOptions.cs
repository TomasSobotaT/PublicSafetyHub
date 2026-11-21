namespace PublicSafetyHub.Core.Configurations;

public interface IFirefightersArticleImportOptions
{
    bool Enabled { get; }

    string ImapHost { get; }

    int ImapPort { get; }

    string Username { get; }

    string Password { get; }
}
