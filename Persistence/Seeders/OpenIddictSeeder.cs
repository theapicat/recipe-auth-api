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
            OpenIddictConstants.Permissions.Endpoints.Revocation,
            OpenIddictConstants.Permissions.GrantTypes.Password,
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles,
            OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess
        };

        // 1. Seed recipe-web-app
        if (!string.IsNullOrWhiteSpace(appSettings.WebAppClientId))
            await SeedOrUpdateClientAsync(manager, appSettings.WebAppClientId, appSettings.WebAppDisplayName,
                allowedPermissions, cancellationToken);

        // 2. Seed recipe-mobile-app
        if (!string.IsNullOrWhiteSpace(appSettings.MobileAppClientId))
            await SeedOrUpdateClientAsync(manager, appSettings.MobileAppClientId, appSettings.MobileAppDisplayName,
                allowedPermissions, cancellationToken);
    }

    // Oppretter klienten hvis den ikke finnes, eller legger til manglende permissions (f.eks.
    // Endpoints.Revocation) på en klient som allerede ble seedet før den fikk lov til å bruke
    // /connect/revoke — uten dette blir et eksisterende lokalt/deployet miljø aldri oppdatert,
    // siden denne seederen ellers bare kjører create-if-missing.
    private static async Task SeedOrUpdateClientAsync(
        IOpenIddictApplicationManager manager,
        string clientId,
        string displayName,
        IReadOnlySet<string> allowedPermissions,
        CancellationToken cancellationToken)
    {
        var existingApplication = await manager.FindByClientIdAsync(clientId, cancellationToken);

        if (existingApplication is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = clientId,
                DisplayName = displayName,
                ClientType = OpenIddictConstants.ClientTypes.Public
            };

            foreach (var permission in allowedPermissions) descriptor.Permissions.Add(permission);

            await manager.CreateAsync(descriptor, cancellationToken);
            return;
        }

        var missingPermissions = new List<string>();
        foreach (var permission in allowedPermissions)
        {
            if (!await manager.HasPermissionAsync(existingApplication, permission, cancellationToken))
                missingPermissions.Add(permission);
        }

        if (missingPermissions.Count == 0) return;

        var updatedDescriptor = new OpenIddictApplicationDescriptor();
        await manager.PopulateAsync(updatedDescriptor, existingApplication, cancellationToken);

        foreach (var permission in missingPermissions) updatedDescriptor.Permissions.Add(permission);

        await manager.UpdateAsync(existingApplication, updatedDescriptor, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}