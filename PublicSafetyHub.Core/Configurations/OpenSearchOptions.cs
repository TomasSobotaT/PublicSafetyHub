namespace PublicSafetyHub.Core.Configurations;

public class OpenSearchOptions : IOpenSearchOptions
{
    public string Url { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }

    public string DocumentsIndex { get; set; }
}
