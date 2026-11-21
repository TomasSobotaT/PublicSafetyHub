namespace PublicSafetyHub.Base.OpenIddictAuth;

public interface IAuthOptions
{
    string Issuer { get; }

    AuthClientOptions[] Clients { get; }

    int AccessTokenMinutes { get; }

    int RefreshTokenDays { get; }

    string SigningPrivateKey { get; }

    string EncryptionKey { get; }
}
