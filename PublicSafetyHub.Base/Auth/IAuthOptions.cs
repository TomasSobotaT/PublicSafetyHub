namespace PublicSafetyHub.Base.Auth;

public interface IAuthOptions
{
    string Issuer { get; }

    string[] ClientIds { get; }

    int AccessTokenMinutes { get; }

    int RefreshTokenDays { get; }

    string SigningCertificatePath { get; }

    string SigningCertificatePassword { get; }

    string EncryptionCertificatePath { get; }

    string EncryptionCertificatePassword { get; }
}
