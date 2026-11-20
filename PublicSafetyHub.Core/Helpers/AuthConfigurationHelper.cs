using PublicSafetyHub.Base.Auth;

namespace PublicSafetyHub.Core.Helpers;

public static class AuthConfigurationHelper
{
    public static void Validate(IAuthOptions options)
    {
        if (!Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer) ||
            issuer.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(issuer.UserInfo) ||
            issuer.AbsolutePath != "/" || !string.IsNullOrEmpty(issuer.Query) || !string.IsNullOrEmpty(issuer.Fragment))
        {
            throw new InvalidOperationException("AuthOptions:Issuer must be an HTTPS origin without a path, query, fragment or user information.");
        }

        if (options.Clients is null || options.Clients.Length == 0 ||
            options.Clients.Any(client => client is null || string.IsNullOrWhiteSpace(client.ClientId)) ||
            options.Clients.Select(client => client.ClientId).Distinct(StringComparer.Ordinal).Count() != options.Clients.Length)
        {
            throw new InvalidOperationException("AuthOptions:Clients must contain unique, nonempty client identifiers.");
        }

        foreach (var client in options.Clients)
        {
            if (client.RedirectUris is null || (client.Enabled && client.RedirectUris.Length == 0) ||
                client.RedirectUris.Distinct(StringComparer.Ordinal).Count() != client.RedirectUris.Length)
            {
                throw new InvalidOperationException($"Configure exact, unique redirect URIs for client '{client.ClientId}' before enabling it.");
            }

            foreach (var address in client.RedirectUris)
            {
                if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
                    Uri.UnescapeDataString(address).Contains('*') || !string.IsNullOrEmpty(uri.Fragment) ||
                    !string.IsNullOrEmpty(uri.UserInfo) ||
                    (uri.Scheme != Uri.UriSchemeHttps && !uri.Scheme.Contains('.')))
                {
                    throw new InvalidOperationException($"Client '{client.ClientId}' requires exact HTTPS or reverse-domain native callback URIs, without wildcards, fragments or user information.");
                }
            }

            if (client.Scopes is null || !client.Scopes.Contains(AuthConstants.ApiScope, StringComparer.Ordinal) ||
                client.Scopes.Any(scope => scope is not ("openid" or "roles" or "offline_access" or AuthConstants.ApiScope)))
            {
                throw new InvalidOperationException($"Client '{client.ClientId}' must include the API scope and may additionally allow openid, roles and offline_access.");
            }
        }
    }
}
