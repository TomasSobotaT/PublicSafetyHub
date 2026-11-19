using OpenIddict.Abstractions;

namespace PublicSafetyHub.Application.ApplicationModels;

internal sealed class AuthTransaction(Dictionary<string, OpenIddictParameter> parameters, DateTime expiresAt)
{
    public Dictionary<string, OpenIddictParameter> Parameters { get; } = parameters;

    public DateTime ExpiresAt { get; } = expiresAt;
}
