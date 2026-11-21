namespace PublicSafetyHub.Base.OpenIddictAuth;

public sealed class AuthorizationLoginSubmission(string intent, string email, string password, string confirmPassword)
{
    public string Intent { get; } = intent;

    public string Email { get; } = email;

    public string Password { get; } = password;

    public string ConfirmPassword { get; } = confirmPassword;
}
