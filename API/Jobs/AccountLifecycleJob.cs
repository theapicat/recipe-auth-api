using Contracts.Events.SystemActions;
using Domain.Enums;
using Domain.Options;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.Context;
using Quartz;

namespace API.Jobs;

[DisallowConcurrentExecution]
public class AccountLifecycleJob(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    IOptions<AccountLifecycleOptions> lifecycleOptions,
    IOptions<AppSettings> appSettings,
    ILogger<AccountLifecycleJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Startet skanning av kontolivssyklus...");

        var options = lifecycleOptions.Value;
        var now = DateTime.UtcNow;

        // Henter spørring for alle bruker-ID-er som har "Admin"-rollen
        var adminUserIds = GetAdminUserIdsQuery();

        // 1. Ubekreftet e-post
        await Process7DaysUnconfirmedRemindersAsync(options, now, adminUserIds);
        await Process14DaysUnconfirmedLockoutsAsync(options, now, adminUserIds);
        await ProcessUnconfirmedDeletionsAsync(options, now, adminUserIds);

        // 2. Inaktivitet
        await Process6MonthsInactivityWarningsAsync(options, now, adminUserIds);
        await Process1YearInactivityLockoutsAsync(options, now, adminUserIds);
        await ProcessInactivityDeletionsAsync(options, now, adminUserIds);

        await dbContext.SaveChangesAsync();
        logger.LogInformation("Fullført skanning av kontolivssyklus.");
    }

    // ------------------------------------------------------------------
    // Skann 1: 7 dagers ubekreftet e-post påminnelse
    // ------------------------------------------------------------------
    private async Task Process7DaysUnconfirmedRemindersAsync(
        AccountLifecycleOptions options, 
        DateTime now, 
        IQueryable<Guid> adminUserIds)
    {
        var cutoff = now.AddDays(-options.ConfirmationReminderDays);

        var usersToRemind = await userManager.Users
            .Where(u => !adminUserIds.Contains(u.Id)
                        && !u.EmailConfirmed
                        && u.CreatedAt <= cutoff
                        && u.Confirmation7DaysReminderSentAt == null)
            .ToListAsync();

        foreach (var user in usersToRemind)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = BuildConfirmationLink(user.Id, token);

            await publishEndpoint.Publish(new Confirmation7DaysReminderEvent
            {
                UserId = user.Id,
                Email = user.Email!,
                Name = GetFullName(user),
                ConfirmationLink = confirmationLink,
                RegisteredAt = user.CreatedAt
            });

            user.Confirmation7DaysReminderSentAt = now;
            user.LastModifiedAt = now;
            await userManager.UpdateAsync(user);

            logger.LogInformation("Publisert 7-dagers påminnelse for bruker {UserId}", user.Id);
        }
    }

    // ------------------------------------------------------------------
    // Skann 2: 14 dagers ubekreftet e-post sperring
    // ------------------------------------------------------------------
    private async Task Process14DaysUnconfirmedLockoutsAsync(
        AccountLifecycleOptions options, 
        DateTime now, 
        IQueryable<Guid> adminUserIds)
    {
        var cutoff = now.AddDays(-options.ConfirmationLockoutDays);

        var usersToLock = await userManager.Users
            .Where(u => !adminUserIds.Contains(u.Id)
                        && !u.EmailConfirmed
                        && u.CreatedAt <= cutoff
                        && u.Confirmation14DaysLockedSentAt == null)
            .ToListAsync();

        foreach (var user in usersToLock)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = BuildConfirmationLink(user.Id, token);

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.LockoutReason = LockoutReason.UnconfirmedEmail14Days;
            user.LockoutReasonDetails = "Konto midlertidig sperret pga. manglende e-postbekreftelse innen 14 dager.";
            user.Confirmation14DaysLockedSentAt = now;
            user.LastModifiedAt = now;

            await userManager.UpdateAsync(user);

            await publishEndpoint.Publish(new Confirmation14DaysReminderEvent
            {
                UserId = user.Id,
                Email = user.Email!,
                Name = GetFullName(user),
                ConfirmationLink = confirmationLink,
                LockedAt = now
            });

            logger.LogInformation("Sperret bruker {UserId} pga. 14 dagers ubekreftet e-post", user.Id);
        }
    }

    // ------------------------------------------------------------------
    // Skann 3: 30 dagers ubekreftet e-post sletting av systemet
    // ------------------------------------------------------------------
    private async Task ProcessUnconfirmedDeletionsAsync(
        AccountLifecycleOptions options, 
        DateTime now, 
        IQueryable<Guid> adminUserIds)
    {
        var cutoff = now.AddDays(-options.ConfirmationDeletionDays);

        var usersToDelete = await userManager.Users
            .Where(u => !adminUserIds.Contains(u.Id)
                        && !u.EmailConfirmed 
                        && u.CreatedAt <= cutoff)
            .ToListAsync();

        foreach (var user in usersToDelete)
        {
            var userId = user.Id;
            var email = user.Email!;
            var name = GetFullName(user);

            var deleteResult = await userManager.DeleteAsync(user);

            if (deleteResult.Succeeded)
            {
                await publishEndpoint.Publish(new UserAccountDeletedBySystemEvent
                {
                    UserId = userId,
                    Email = email,
                    Name = name,
                    DeletionReason = "Kontoen ble slettet fordi e-postadressen ikke ble bekreftet innen 30 dager (se brukervilkår § 3).",
                    DeletedAt = now
                });

                logger.LogInformation("Slettet bruker {UserId} fra systemet (30 dager ubekreftet)", userId);
            }
        }
    }

    // ------------------------------------------------------------------
    // Skann 4: 6 måneders inaktivitetsvarsel
    // ------------------------------------------------------------------
    private async Task Process6MonthsInactivityWarningsAsync(
        AccountLifecycleOptions options, 
        DateTime now, 
        IQueryable<Guid> adminUserIds)
    {
        var cutoff = now.AddMonths(-options.InactivityWarningMonths);

        var usersToWarn = await userManager.Users
            .Where(u => !adminUserIds.Contains(u.Id)
                        && u.EmailConfirmed
                        && (u.LastLoginAt ?? u.CreatedAt) <= cutoff
                        && u.InactivityWarning6MonthsSentAt == null)
            .ToListAsync();

        foreach (var user in usersToWarn)
        {
            user.InactivityWarning6MonthsSentAt = now;
            user.LastModifiedAt = now;
            await userManager.UpdateAsync(user);

            logger.LogInformation("Satt 6 måneders inaktivitetsmerke for bruker {UserId}", user.Id);
        }
    }

    // ------------------------------------------------------------------
    // Skann 5: 1 års inaktivitets sperring
    // ------------------------------------------------------------------
    private async Task Process1YearInactivityLockoutsAsync(
        AccountLifecycleOptions options, 
        DateTime now, 
        IQueryable<Guid> adminUserIds)
    {
        var cutoff = now.AddYears(-options.InactivityLockoutYears);

        var usersToLock = await userManager.Users
            .Where(u => !adminUserIds.Contains(u.Id)
                        && u.EmailConfirmed
                        && (u.LastLoginAt ?? u.CreatedAt) <= cutoff
                        && u.Inactivity1YearLockedSentAt == null)
            .ToListAsync();

        foreach (var user in usersToLock)
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.LockoutReason = LockoutReason.Inactivity1Year;
            user.LockoutReasonDetails = "Konto sperret pga. inaktivitet i over 1 år uden innlogging.";
            user.Inactivity1YearLockedSentAt = now;
            user.LastModifiedAt = now;

            await userManager.UpdateAsync(user);

            logger.LogInformation("Sperret bruker {UserId} pga. 1 års inaktivitet", user.Id);
        }
    }

    // ------------------------------------------------------------------
    // Skann 6: Permanent sletting etter 1 års inaktivitet + 30 dagers sperre
    // ------------------------------------------------------------------
    private async Task ProcessInactivityDeletionsAsync(
        AccountLifecycleOptions options, 
        DateTime now, 
        IQueryable<Guid> adminUserIds)
    {
        var cutoff = now.AddDays(-options.InactivityDeletionDays);

        var usersToDelete = await userManager.Users
            .Where(u => !adminUserIds.Contains(u.Id)
                        && u.EmailConfirmed
                        && u.Inactivity1YearLockedSentAt != null
                        && u.Inactivity1YearLockedSentAt <= cutoff)
            .ToListAsync();

        foreach (var user in usersToDelete)
        {
            var userId = user.Id;
            var email = user.Email!;
            var name = GetFullName(user);

            var deleteResult = await userManager.DeleteAsync(user);

            if (deleteResult.Succeeded)
            {
                await publishEndpoint.Publish(new UserAccountDeletedBySystemEvent
                {
                    UserId = userId,
                    Email = email,
                    Name = name,
                    DeletionReason = "Kontoen ble permanent slettet pga. inaktivitet i over 1 år og manglende reaktivering innen 30-dagers sperreperioden (se brukervilkår § 3).",
                    DeletedAt = now
                });

                logger.LogInformation("Slettet inaktiv bruker {UserId} fra systemet", userId);
            }
        }
    }

    // ------------------------------------------------------------------
    // Hjelpemetoder
    // ------------------------------------------------------------------
    private IQueryable<Guid> GetAdminUserIdsQuery()
    {
        return dbContext.UserRoles
            .Join(
                dbContext.Roles.Where(r => r.Name == "Admin"),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => ur.UserId
            );
    }

    private string BuildConfirmationLink(Guid userId, string token)
    {
        var baseUrl = appSettings.Value.FrontendUrl.TrimEnd('/');
        return $"{baseUrl}/confirm-email?userId={userId}&token={Uri.EscapeDataString(token)}";
    }

    private static string GetFullName(ApplicationUser user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;
    }
}