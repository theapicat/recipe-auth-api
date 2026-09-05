using System.Security.Claims;
using Contracts.Events.AdminActions;
using Contracts.Events.UserActions;
using Domain.DTOs.Admin;
using Domain.Enums;
using Domain.Options;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Persistence.Context;

namespace API.Controllers;

[ApiController]
[Route("api/auth/admin")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminController(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint,
    IOptions<AppSettings> appSettings) : ControllerBase
{
    // --- 1. HENT BRUKERLISTE ---
    // URL: GET /api/auth/admin/users
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await userManager.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var userListItems = new List<AdminUserListItemDto>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var isLocked = await userManager.IsLockedOutAsync(user);
            var logins = await userManager.GetLoginsAsync(user);

            userListItems.Add(new AdminUserListItemDto
            {
                UserId = user.Id.ToString(),
                Email = user.Email ?? string.Empty,
                FullName = GetFullName(user),
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? "user",
                IsEmailConfirmed = user.EmailConfirmed,
                IsLocked = isLocked,
                IsGoogleAccount = logins.Any(l => l.LoginProvider == "Google"),
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }

        return Ok(userListItems);
    }

    // --- 2. HENT DETALJERT BRUKERPROFIL FOR ADMIN ---
    // URL: GET /api/auth/admin/users/{id}
    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUserDetails(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        var roles = await userManager.GetRolesAsync(user);
        var isLocked = await userManager.IsLockedOutAsync(user);
        var hasPassword = await userManager.HasPasswordAsync(user);
        var logins = await userManager.GetLoginsAsync(user);

        var details = new AdminUserDetailsDto
        {
            UserId = user.Id.ToString(),
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = roles.FirstOrDefault() ?? "user",
            HasPassword = hasPassword,
            IsGoogleAccount = logins.Any(l => l.LoginProvider == "Google"),
            IsEmailConfirmed = user.EmailConfirmed,
            WelcomeCompleted = user.WelcomeCompleted,
            IsLocked = isLocked,
            LockoutEnd = user.LockoutEnd,
            LockoutReason = user.LockoutReason.ToString(),
            LockoutReasonDetails = user.LockoutReasonDetails,
            AccessFailedCount = user.AccessFailedCount,
            CreatedAt = user.CreatedAt,
            LastModifiedAt = user.LastModifiedAt,
            LastLoginAt = user.LastLoginAt,

            Confirmation7DaysReminderSentAt = user.Confirmation7DaysReminderSentAt,
            Confirmation14DaysLockedSentAt = user.Confirmation14DaysLockedSentAt,
            InactivityWarning6MonthsSentAt = user.InactivityWarning6MonthsSentAt,
            Inactivity1YearLockedSentAt = user.Inactivity1YearLockedSentAt
        };

        return Ok(details);
    }

    // --- 3. REDIGER BRUKERPERSONALIA ---
    // URL: PUT /api/auth/admin/users
    [HttpPut("users")]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateUser([FromBody] AdminUpdateUserRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        var oldEmail = user.Email ?? string.Empty;
        var emailChanged = !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;

        if (emailChanged)
        {
            user.Email = request.Email;
            user.UserName = request.Email;
        }

        user.LastModifiedAt = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        // Publiserer event om at administrator har endret brukerprofilen
        await publishEndpoint.Publish(new UserUpdatedByAdminEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = GetFullName(user),
            OldEmail = oldEmail,
            NewEmail = request.Email,
            UpdatedAt = DateTime.UtcNow
        });

        return Ok(new { Message = "Brukerinformasjonen ble oppdatert." });
    }

    // --- 4. SPERR BRUKER MANUELT (Lockout) ---
    // URL: POST /api/auth/admin/users/lock
    [HttpPost("users/lock")]
    [Consumes("application/json")]
    public async Task<IActionResult> LockUser([FromBody] LockUserRequest request)
    {
        if (!Guid.TryParse(request.UserId, out var targetGuid))
            return BadRequest(new { Message = "Ugyldig bruker-ID oppgitt." });

        var currentAdmin = await GetCurrentAdminAsync();
        if (currentAdmin?.Id == targetGuid)
            return BadRequest(new { Message = "Du kan ikke låse din egen administratorkonto." });

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        var reasonDetails = request.ReasonDetails ?? "Kontoen ble sperret av en administrator.";

        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        user.LockoutReason = LockoutReason.ManualAdminLock;
        user.LockoutReasonDetails = reasonDetails;
        user.LastModifiedAt = DateTime.UtcNow;

        await userManager.UpdateAsync(user);

        // Publiserer event om sperring
        await publishEndpoint.Publish(new UserLockedByAdminEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = GetFullName(user),
            ReasonDetails = reasonDetails,
            LockedAt = DateTime.UtcNow
        });

        return Ok(new { Message = $"Kontoen til {user.Email} har blitt sperret." });
    }

    // --- 5. GJENÅPNE SPERRET BRUKER (Unlock) ---
    // URL: POST /api/auth/admin/users/unlock
    [HttpPost("users/unlock")]
    [Consumes("application/json")]
    public async Task<IActionResult> UnlockUser([FromBody] UnlockUserRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);

        user.LockoutReason = LockoutReason.None;
        user.LockoutReasonDetails = null;
        user.LastModifiedAt = DateTime.UtcNow;

        await userManager.UpdateAsync(user);

        // Publiserer event om gjenåpning
        await publishEndpoint.Publish(new UserUnlockedByAdminEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = GetFullName(user),
            UnlockedAt = DateTime.UtcNow
        });

        return Ok(new { Message = $"Sperren for {user.Email} har blitt fjernet." });
    }

    // --- 6. MANUELL BEKREFTELSE AV E-POST ---
    // URL: POST /api/auth/admin/users/confirm-email
    [HttpPost("users/confirm-email")]
    [Consumes("application/json")]
    public async Task<IActionResult> ManuallyConfirmEmail([FromBody] ResendConfirmationAdminRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        if (user.EmailConfirmed)
            return BadRequest(new { Message = "E-posten til denne brukeren er allerede bekreftet." });

        user.EmailConfirmed = true;

        if (user.LockoutReason == LockoutReason.UnconfirmedEmail14Days)
        {
            await userManager.SetLockoutEndDateAsync(user, null);
            user.LockoutReason = LockoutReason.None;
            user.LockoutReasonDetails = null;
        }

        user.LastModifiedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        // Publiserer event om manuell bekreftelse
        await publishEndpoint.Publish(new EmailManuallyConfirmedByAdminEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = GetFullName(user),
            ConfirmedAt = DateTime.UtcNow
        });

        return Ok(new { Message = $"E-postadressen til {user.Email} er nå manuelt bekreftet." });
    }

    // --- 7. SEND BEKREFTELSESE-POST PÅ VEGNE AV BRUKER ---
    // URL: POST /api/auth/admin/users/resend-confirmation
    [HttpPost("users/resend-confirmation")]
    [Consumes("application/json")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationAdminRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        if (user.EmailConfirmed)
            return BadRequest(new { Message = "E-posten er allerede bekreftet." });

        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var baseUrl = appSettings.Value.FrontendUrl.TrimEnd('/');
        var confirmationLink = $"{baseUrl}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(confirmationToken)}";

        await publishEndpoint.Publish(new ResendEmailConfirmationRequestedEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = GetFullName(user),
            ConfirmationLink = confirmationLink,
            RequestedAt = DateTime.UtcNow
        });

        return Ok(new { Message = $"Ny bekreftelseslenke har blitt sendt til {user.Email}." });
    }

    // --- 8. SEND PASSORD-TILBAKESTILLING PÅ VEGNE AV BRUKER ---
    // URL: POST /api/auth/admin/users/reset-password-request
    [HttpPost("users/reset-password-request")]
    [Consumes("application/json")]
    public async Task<IActionResult> SendPasswordReset([FromBody] ResetPasswordAdminRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var baseUrl = appSettings.Value.FrontendUrl.TrimEnd('/');
        var resetLink = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(resetToken)}";

        await publishEndpoint.Publish(new PasswordResetRequestedEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = GetFullName(user),
            ResetLink = resetLink,
            RequestedAt = DateTime.UtcNow
        });

        return Ok(new { Message = $"Lenke for tilbakestilling av passord har blitt sendt til {user.Email}." });
    }

    // --- 9. SLETT BRUKER (Admin-sletting) ---
    // URL: POST /api/auth/admin/users/delete
    [HttpPost("users/delete")]
    [Consumes("application/json")]
    public async Task<IActionResult> DeleteUser([FromBody] DeleteUserAdminRequest request)
    {
        if (!Guid.TryParse(request.UserId, out var targetGuid))
            return BadRequest(new { Message = "Ugyldig bruker-ID oppgitt." });

        var currentAdmin = await GetCurrentAdminAsync();
        if (currentAdmin?.Id == targetGuid)
            return BadRequest(new { Message = "Du kan ikke slette din egen administratorkonto." });

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        var userId = user.Id;
        var email = user.Email ?? string.Empty;
        var name = GetFullName(user);

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        // Publiserer dedikert admin-slettingsevent
        await publishEndpoint.Publish(new UserAccountDeletedByAdminEvent
        {
            UserId = userId,
            Email = email,
            Name = name,
            DeletedAt = DateTime.UtcNow
        });

        return Ok(new { Message = $"Bruker {email} har blitt permanent slettet." });
    }

    // --- HJELPEMETODER ---
    private async Task<ApplicationUser?> GetCurrentAdminAsync()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue(OpenIddictConstants.Claims.Subject);

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return null;

        return await userManager.FindByIdAsync(userId.ToString());
    }

    private static string GetFullName(ApplicationUser user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;
    }
}