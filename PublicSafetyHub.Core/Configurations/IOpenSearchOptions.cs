namespace PublicSafetyHub.Core.Configurations;

public interface IOpenSearchOptions
{
    string Url { get; }

    string Username { get; }

    string Password { get; }

    string DocumentsIndex { get; }
}
