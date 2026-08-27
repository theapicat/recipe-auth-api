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

        // 1. Opprett roller (små bokstaver)
        string[] roles = ["admin", "user"];
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        // 2. Alltid opprett Admin-bruker (passord fra appsettings)
        var adminEmail = config["AdminUser:Email"] ?? "admin@kjoekkenhylla.local";
        var adminPassword = config["AdminUser:Password"];

        if (!string.IsNullOrEmpty(adminPassword) && await userManager.FindByEmailAsync(adminEmail) == null)
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
                await userManager.AddToRoleAsync(adminUser, "admin");
            }
        }

        // 3. Opprett testbrukere KUN i Dev-modus
        if (env.IsDevelopment())
        {
            var devEmail = "test@example.com";
            if (await userManager.FindByEmailAsync(devEmail) == null)
            {
                var devUser = new ApplicationUser
                {
                    UserName = devEmail,
                    Email = devEmail,
                    FirstName = "Test",
                    LastName = "Bruker",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(devUser, "Dev12345!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(devUser, "user");
                }
            }
        }
    }
}