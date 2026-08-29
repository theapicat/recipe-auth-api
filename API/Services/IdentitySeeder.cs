using API.Context;
using Microsoft.AspNetCore.Identity;

namespace API.Services;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        // 1. Opprett roller (PascalCase så de matcher frontend)
        string[] roles = ["Admin", "User"];
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        // 2. Opprett Admin-bruker (Standardiser på admin@recipeapp.com)
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
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // 3. Opprett testbrukere KUN i Dev-modus (User 1 og User 2)
        if (env.IsDevelopment())
        {
            var devUsers = new[]
            {
                new { Email = "user1@example.com", Password = "DevUser123!", FirstName = "Test", LastName = "Bruker 1" },
                new { Email = "user2@example.com", Password = "DevUser123!", FirstName = "Test", LastName = "Bruker 2" }
            };

            foreach (var dev in devUsers)
            {
                if (await userManager.FindByEmailAsync(dev.Email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = dev.Email,
                        Email = dev.Email,
                        FirstName = dev.FirstName,
                        LastName = dev.LastName,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, dev.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "User");
                    }
                }
            }
        }
    }
}