using System.Text.Json;
using Domain.Enums;
using Domain.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Persistence.Context;

namespace Persistence.Seeders;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminOptions = scope.ServiceProvider.GetRequiredService<IOptions<AdminUserOptions>>().Value;
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
        if (string.IsNullOrWhiteSpace(adminOptions.Email) || string.IsNullOrWhiteSpace(adminOptions.Password))
        {
            throw new InvalidOperationException("Konfigurasjon for 'AdminUser' (Email/Password) mangler i appsettings.");
        }

        var adminEmail = adminOptions.Email;
        var adminPassword = adminOptions.Password;

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
                LastLoginAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Kunne ikke opprette adminbruker: {errors}");
            }
        }

        // 3. Testbrukere for Dev-modus fra JSON-fil
        if (env.IsDevelopment())
        {
            await SeedDevUsersFromJsonAsync(userManager, env);
        }
    }

    private static async Task SeedDevUsersFromJsonAsync(UserManager<ApplicationUser> userManager, IHostEnvironment env)
    {
        var jsonFilePath = Path.Combine(AppContext.BaseDirectory, "Seeders", "seed-users.json");

        if (!File.Exists(jsonFilePath))
        {
            // Sjekk om filen ligger i rot/prosjektmappe dersom den ikke finner den i output-mappen
            jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Seeders", "seed-users.json");
            if (!File.Exists(jsonFilePath))
                return;
        }

        var jsonContent = await File.ReadAllTextAsync(jsonFilePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var seedUsers = JsonSerializer.Deserialize<List<SeedUserDto>>(jsonContent, options);

        if (seedUsers == null || seedUsers.Count == 0)
            return;

        var now = DateTime.UtcNow;

        foreach (var seedUser in seedUsers)
        {
            if (await userManager.FindByEmailAsync(seedUser.Email) != null)
                continue;

            var createdAt = seedUser.CreatedAtDaysAgo.HasValue
                ? now.AddDays(-seedUser.CreatedAtDaysAgo.Value)
                : now;

            DateTime? lastLoginAt = seedUser.LastLoginDaysAgo.HasValue
                ? now.AddDays(-seedUser.LastLoginDaysAgo.Value)
                : null;

            // Beregn datoer for tidslinjen basert på brukervilkårene
            DateTime? reminder7dSentAt = null;
            DateTime? lockout14dSentAt = null;
            DateTime? inactivity6mSentAt = null;
            DateTime? inactivity1ySentAt = null;

            if (!seedUser.EmailConfirmed && seedUser.CreatedAtDaysAgo >= 7)
                reminder7dSentAt = createdAt.AddDays(7);

            if (!seedUser.EmailConfirmed && seedUser.CreatedAtDaysAgo >= 14)
                lockout14dSentAt = createdAt.AddDays(14);

            var checkInactivityDate = lastLoginAt ?? createdAt;
            var daysInactive = (now - checkInactivityDate).TotalDays;

            if (daysInactive >= 180)
                inactivity6mSentAt = checkInactivityDate.AddDays(180);

            if (daysInactive >= 365)
                inactivity1ySentAt = checkInactivityDate.AddDays(365);

            LockoutReason lockoutReasonEnum = LockoutReason.None;
            if (!string.IsNullOrWhiteSpace(seedUser.LockoutReason))
            {
                Enum.TryParse(seedUser.LockoutReason, out lockoutReasonEnum);
            }

            var user = new ApplicationUser
            {
                UserName = seedUser.Email,
                Email = seedUser.Email,
                FirstName = seedUser.FirstName,
                LastName = seedUser.LastName,
                EmailConfirmed = seedUser.EmailConfirmed,
                WelcomeCompleted = seedUser.WelcomeCompleted,
                CreatedAt = createdAt,
                LastModifiedAt = createdAt,
                LastLoginAt = lastLoginAt,
                Confirmation7DaysReminderSentAt = reminder7dSentAt,
                Confirmation14DaysLockedSentAt = lockout14dSentAt,
                InactivityWarning6MonthsSentAt = inactivity6mSentAt,
                Inactivity1YearLockedSentAt = inactivity1ySentAt,
                LockoutReason = lockoutReasonEnum,
                LockoutReasonDetails = seedUser.LockoutReasonDetails
            };

            IdentityResult result;

            if (seedUser.IsGoogleAccount)
            {
                // Opprettes uten lokal passordhash
                result = await userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "User");
                    await userManager.AddLoginAsync(user, new UserLoginInfo("Google", $"google-dev-key-{user.Email}", "Google"));
                }
            }
            else
            {
                var password = string.IsNullOrWhiteSpace(seedUser.Password) ? "DevUser123!" : seedUser.Password;
                result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "User");
                }
            }

            // Håndter låsing dersom kilden sier at den er låst
            if (result.Succeeded && seedUser.IsLocked)
            {
                await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }
        }
    }

    private class SeedUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; } = true;
        public bool WelcomeCompleted { get; set; } = true;
        public string? Password { get; set; }
        public bool IsGoogleAccount { get; set; } = false;
        public bool IsLocked { get; set; } = false;
        public string? LockoutReason { get; set; }
        public string? LockoutReasonDetails { get; set; }
        public int? CreatedAtDaysAgo { get; set; }
        public int? LastLoginDaysAgo { get; set; }
    }
}