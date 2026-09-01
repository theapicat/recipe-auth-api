using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistence.Context;

namespace Persistence.Seeders;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        // 1. Opprett roller
        string[] roles = ["Admin", "User"];
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        // 2. Opprett Admin-bruker
        var adminEmail = config["AdminUser:Email"] ?? "admin@recipeapp.com";
        var adminPassword = config["AdminUser:Password"] ?? "AdminSuperSecretPassword123!";

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Admin",
                EmailConfirmed = true,
                WelcomeCompleted = true,
                LastLoginAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // 3. Testbrukere for Dev-modus
        if (env.IsDevelopment())
        {
            // A. Fullstendig bekreftet standardbruker
            var confirmedEmail = "confirmed@example.com";
            if (await userManager.FindByEmailAsync(confirmedEmail) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = confirmedEmail,
                    Email = confirmedEmail,
                    FirstName = "Ola",
                    LastName = "Nordmann",
                    EmailConfirmed = true,
                    WelcomeCompleted = true,
                    LastLoginAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, "DevUser123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "User");
                }
            }

            // B. Ubekreftet bruker (skal vise advarselsbanner/resend-skjema)
            var unconfirmedEmail = "unconfirmed@example.com";
            if (await userManager.FindByEmailAsync(unconfirmedEmail) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = unconfirmedEmail,
                    Email = unconfirmedEmail,
                    FirstName = "Kari",
                    LastName = "Ubekreftet",
                    EmailConfirmed = false,
                    WelcomeCompleted = true,
                    LastLoginAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, "DevUser123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "User");
                }
            }

            // C. Helt ny bruker (ubekreftet e-post + ufullført velkomstsone)
            var newEmail = "newuser@example.com";
            if (await userManager.FindByEmailAsync(newEmail) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = newEmail,
                    Email = newEmail,
                    FirstName = "Pelle",
                    LastName = "Nykomling",
                    EmailConfirmed = false,
                    WelcomeCompleted = false
                };

                var result = await userManager.CreateAsync(user, "DevUser123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "User");
                }
            }

            // D. Google-bruker (Inget lokalt passord, koblet via External Login)
            var googleEmail = "googleuser@example.com";
            if (await userManager.FindByEmailAsync(googleEmail) == null)
            {
                var user = new ApplicationUser
                {
                    UserName = googleEmail,
                    Email = googleEmail,
                    FirstName = "Google",
                    LastName = "Bruker",
                    EmailConfirmed = true,
                    WelcomeCompleted = true,
                    LastLoginAt = DateTime.UtcNow
                };

                // Opprettes uten passord
                var result = await userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "User");

                    // Kobler til Google som ekstern innloggingsleverandør
                    await userManager.AddLoginAsync(user, new UserLoginInfo("Google", "google-dev-provider-key-12345", "Google"));
                }
            }
        }
    }
}