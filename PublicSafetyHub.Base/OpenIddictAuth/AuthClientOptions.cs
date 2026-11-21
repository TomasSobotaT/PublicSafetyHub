namespace PublicSafetyHub.Base.OpenIddictAuth;

public class AuthClientOptions
{
    public string ClientId { get; set; }

    public bool Enabled { get; set; }

    public string[] RedirectUris { get; set; }

    public string[] Scopes { get; set; }
}
