using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using PublicSafetyHub.Base.Auth;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace PublicSafetyHub.Application.Services;

public class AuthClientInitializer(IServiceScopeFactory scopeFactory, IAuthOptions options) : IHostedService
{
    public async Task StartAsync(CancellationToken _)
    {
        using var scope = scopeFactory.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var clientId in options.ClientIds.Distinct(StringComparer.Ordinal))
        {
            if (await manager.FindByClientIdAsync(clientId) is not null)
            {
                continue;
            }

            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                DisplayName = clientId,
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.Prefixes.Scope + AuthConstants.ApiScope
                }
            });
        }
    }

    public Task StopAsync(CancellationToken _)
    {
        return Task.CompletedTask;
    }
}
