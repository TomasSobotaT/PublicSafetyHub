namespace PublicSafetyHub.Base.Auth;

public interface IAuthOptions
{
    string Issuer { get; }

    string[] ClientIds { get; }

    int AccessTokenMinutes { get; }

    int RefreshTokenDays { get; }

    string SigningPrivateKey { get; }

    string EncryptionKey { get; }
}
