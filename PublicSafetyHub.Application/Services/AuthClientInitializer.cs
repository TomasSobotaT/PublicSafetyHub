using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using PublicSafetyHub.Base.OpenIddictAuth;
using PublicSafetyHub.Core.Helpers;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace PublicSafetyHub.Application.Services;

public class AuthClientInitializer(IServiceScopeFactory scopeFactory, IAuthOptions authOptions) : IHostedService
{
    public async Task StartAsync(CancellationToken _)
    {
        AuthConfigurationHelper.Validate(authOptions);
        using var scope = scopeFactory.CreateScope();
        var openIddictApplicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var client in authOptions.Clients)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = client.ClientId,
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                DisplayName = client.ClientId
            };

            if (client.Enabled)
            {
                descriptor.RedirectUris.UnionWith(client.RedirectUris.Select(address => new Uri(address)));
                descriptor.Permissions.UnionWith([
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code
                ]);
                descriptor.Permissions.UnionWith(client.Scopes
                    .Where(value => value is not (Scopes.OpenId or Scopes.OfflineAccess))
                    .Select(value => Permissions.Prefixes.Scope + value));
                descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
            }

            var application = await openIddictApplicationManager.FindByClientIdAsync(client.ClientId);

            if (application is null)
            {
                await openIddictApplicationManager.CreateAsync(descriptor);
            }
            else
            {
                await openIddictApplicationManager.UpdateAsync(application, descriptor);
            }
        }
    }

    public Task StopAsync(CancellationToken _)
    {
        return Task.CompletedTask;
    }
}
