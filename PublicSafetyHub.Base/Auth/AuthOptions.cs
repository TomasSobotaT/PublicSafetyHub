namespace PublicSafetyHub.Base.Auth;

public class AuthOptions : IAuthOptions
{
    public string Issuer { get; set; }

    public string[] ClientIds { get; set; }

    public int AccessTokenMinutes { get; set; }

    public int RefreshTokenDays { get; set; }

    public string SigningPrivateKey { get; set; }

    public string EncryptionKey { get; set; }
}
