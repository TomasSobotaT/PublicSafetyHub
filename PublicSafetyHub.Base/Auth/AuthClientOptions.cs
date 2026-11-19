namespace PublicSafetyHub.Base.Auth;

public class AuthClientOptions
{
    public string ClientId { get; set; }

    public bool Enabled { get; set; }

    public string[] RedirectUris { get; set; } = [];

    public string[] Scopes { get; set; } = ["openid", "roles", "offline_access", AuthConstants.ApiScope];
}
