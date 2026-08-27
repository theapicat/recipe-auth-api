using OpenIddict.Abstractions;
using API.Context;

namespace API.Services;

public class OpenIddictSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public OpenIddictSeeder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);
        
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var allowedPermissions = new List<string>
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
        if (await manager.FindByClientIdAsync("recipe-web-app", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "recipe-web-app",
                DisplayName = "Kjøkkenhylla Web App",
                Permissions = { allowedPermissions[0], allowedPermissions[1], allowedPermissions[2], allowedPermissions[3], allowedPermissions[4], allowedPermissions[5], allowedPermissions[6] }
            }, cancellationToken);
        }

        // 2. Seed recipe-mobile-app
        if (await manager.FindByClientIdAsync("recipe-mobile-app", cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "recipe-mobile-app",
                DisplayName = "Kjøkkenhylla Mobil App",
                Permissions = { allowedPermissions[0], allowedPermissions[1], allowedPermissions[2], allowedPermissions[3], allowedPermissions[4], allowedPermissions[5], allowedPermissions[6] }
            }, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}