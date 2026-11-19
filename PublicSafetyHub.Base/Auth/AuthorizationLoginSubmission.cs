namespace PublicSafetyHub.Base.Auth;

// Kept only for the current request. Do not generate a ToString containing credentials.
public sealed class AuthorizationLoginSubmission(string intent, string email, string password, string confirmPassword)
{
    public string Intent { get; } = intent;
    public string Email { get; } = email;
    public string Password { get; } = password;
    public string ConfirmPassword { get; } = confirmPassword;
}
