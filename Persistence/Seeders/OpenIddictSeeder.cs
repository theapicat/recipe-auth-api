using Domain.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Persistence.Context;

namespace Persistence.Seeders;

public class OpenIddictSeeder(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);
        
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var appSettings = scope.ServiceProvider.GetRequiredService<IOptions<AppSettings>>().Value;

        var allowedPermissions = new HashSet<string>
        {
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.GrantTypes.Password,
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles,
            OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess
        };

        // 1. Seed recipe-web-app
        if (!string.IsNullOrWhiteSpace(appSettings.WebAppClientId) &&
            await manager.FindByClientIdAsync(appSettings.WebAppClientId, cancellationToken) is null)
        {
            var webAppDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = appSettings.WebAppClientId,
                DisplayName = appSettings.WebAppDisplayName,
                ClientType = OpenIddictConstants.ClientTypes.Public
            };

            foreach (var permission in allowedPermissions)
            {
                webAppDescriptor.Permissions.Add(permission);
            }

            await manager.CreateAsync(webAppDescriptor, cancellationToken);
        }

        // 2. Seed recipe-mobile-app
        if (!string.IsNullOrWhiteSpace(appSettings.MobileAppClientId) &&
            await manager.FindByClientIdAsync(appSettings.MobileAppClientId, cancellationToken) is null)
        {
            var mobileAppDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = appSettings.MobileAppClientId,
                DisplayName = appSettings.MobileAppDisplayName,
                ClientType = OpenIddictConstants.ClientTypes.Public
            };

            foreach (var permission in allowedPermissions)
            {
                mobileAppDescriptor.Permissions.Add(permission);
            }

            await manager.CreateAsync(mobileAppDescriptor, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}