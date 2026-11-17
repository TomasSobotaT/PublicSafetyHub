namespace PublicSafetyHub.Base.Auth;

public class AuthOptions : IAuthOptions
{
    public string Issuer { get; set; }

    public string[] ClientIds { get; set; } = ["public-safety-hub"];

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 14;

    public string SigningCertificatePath { get; set; }

    public string SigningCertificatePassword { get; set; }

    public string EncryptionCertificatePath { get; set; }

    public string EncryptionCertificatePassword { get; set; }
}
