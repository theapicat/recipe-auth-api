using System.Security.Claims;
using Application.Mediator.Auth.GoogleCallback;
using Application.Mediator.Auth.Register;
using Contracts.Events;
using Domain.DTOs;
using Domain.Enums;
using Domain.Options;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using Persistence.Context;

namespace API.Controllers;

[ApiController]
[Route("api/auth/account")]
public class AccountController(
    IMediator mediator,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IPublishEndpoint publishEndpoint,
    IOptions<AppSettings> appSettings) : ControllerBase
{
    // --- 1. REGISTRERING (Anonym) ---
    // URL: POST /api/auth/account/register
    [HttpPost("register")]
    [Consumes("application/x-www-form-urlencoded", "application/json")]
    public async Task<IActionResult> Register([FromForm] RegisterRequest request)
    {
        var command = new RegisterUserCommand(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName
        );

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Errors != null)
            {
                return BadRequest(result.Errors);
            }

            return BadRequest(new { Message = result.ErrorMessage });
        }

        return Ok(result.UserProfile);
    }

    // --- 2. HENT MIN PROFIL (Innlogget) ---
    // URL: GET /api/auth/account/me
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GetProfile()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        var response = await MapToUserProfileResponseAsync(user);
        return Ok(response);
    }

    // --- 3. OPPDATER PROFIL (Innlogget) ---
    // URL: PUT /api/auth/account/profile
    [HttpPut("profile")]
    [Consumes("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        // Beskyttelse mot endring av systemadministrator
        if (await userManager.IsInRoleAsync(user, "Admin"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Profilinformasjonen til systemadministrator er låst og kan ikke endres." });
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.LastModifiedAt = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var response = await MapToUserProfileResponseAsync(user);
        return Ok(response);
    }

    // --- 4. BYTT PASSORD (Innlogget - Brukere med eksisterende passord) ---
    // URL: POST /api/auth/account/change-password
    [HttpPost("change-password")]
    [Consumes("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        user.LastModifiedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        // Publiserer PasswordChangedEvent
        await PublishPasswordChangedEventAsync(user);

        return Ok(new { Message = "Passord ble endret med hell." });
    }

    // --- 5. OPPRETT PASSORD (Innlogget - For Google-brukere uten lokalt passord) ---
    // URL: POST /api/auth/account/set-password
    [HttpPost("set-password")]
    [Consumes("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        var hasPassword = await userManager.HasPasswordAsync(user);
        if (hasPassword)
        {
            return BadRequest(new { Message = "Kontoen din har allerede et passord. Bruk change-password i stedet." });
        }

        var result = await userManager.AddPasswordAsync(user, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        user.LastModifiedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        // Publiserer PasswordChangedEvent
        await PublishPasswordChangedEventAsync(user);

        return Ok(new { Message = "Passord har blitt opprettet for din konto." });
    }

    // --- 6. FULLFØR VELKOMST (Innlogget) ---
    // URL: GET /api/auth/account/complete-welcome
    [HttpGet("complete-welcome")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> CompleteWelcome()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        user.WelcomeCompleted = true;
        user.LastModifiedAt = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var response = await MapToUserProfileResponseAsync(user);
        return Ok(response);
    }

    // --- 7. SEND BEKREFTELSESE-POST PÅ NYTT (Innlogget) ---
    // URL: POST /api/auth/account/resend-confirmation
    [HttpPost("resend-confirmation")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> ResendConfirmation()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        if (user.EmailConfirmed)
        {
            return BadRequest(new { Message = "E-postadressen din er allerede bekreftet." });
        }

        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

        return Ok(new { Message = "Ny bekreftelseslenke har blitt sendt til din e-postadresse.", ConfirmationToken = confirmationToken });
    }

    // --- 8. BEKREFT E-POST (Anonym - Brukes fra lenken i e-posten) ---
    // URL: POST /api/auth/account/confirm-email
    [HttpPost("confirm-email")]
    [Consumes("application/json")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            return BadRequest(new { Message = "Ugyldig eller utløpt bekreftelseskode.", Errors = result.Errors });
        }

        // Fjerner sperren dersom brukeren var låst pga. ubekreftet e-post
        if (await userManager.IsLockedOutAsync(user))
        {
            await userManager.SetLockoutEndDateAsync(user, null);
            user.LockoutReason = LockoutReason.None;
            user.LockoutReasonDetails = null;
        }

        user.LastModifiedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return Ok(new { Message = "E-postadressen din er bekreftet!" });
    }

    // --- 9. RECOVER / GLEMT PASSORD (Anonym) ---
    // URL: POST /api/auth/account/recover
    [HttpPost("recover")]
    [Consumes("application/json")]
    public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordRequest request)
    {
        Console.WriteLine($"DEBUG :: Received recover password request");
        var user = await userManager.FindByEmailAsync(request.Email);
        
        // Av sikkerhetsgrunner returneres samme respons uavhengig av om brukeren eksisterer
        if (user == null)
        {
            Console.WriteLine($"DEBUG :: User {request.Email} not found");
            return Ok(new { Message = "Dersom e-posten er registrert, har instruksjoner om tilbakestilling blitt sendt." });
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

        var baseUrl = appSettings.Value.FrontendUrl.TrimEnd('/');
        var resetLink = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(resetToken)}";
        
        Console.WriteLine($"DEBUG :: Password reset link: {resetLink}");
        await publishEndpoint.Publish(new PasswordResetRequestedEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = GetFullName(user),
            ResetLink = resetLink,
            RequestedAt = DateTime.UtcNow
        });
            
        Console.WriteLine($"DEBUG :: All ok, return ok");
        return Ok(new { Message = "Dersom e-posten er registrert, har instruksjoner om tilbakestilling blitt sendt." });
    }

    // --- 10. TILBAKESTILL PASSORD (Anonym - Brukes fra lenken i e-posten) ---
    // URL: POST /api/auth/account/reset-password
    [HttpPost("reset-password")]
    [Consumes("application/json")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(new { Message = "Tilbakestilling mislyktes.", Errors = result.Errors });
        }

        user.LastModifiedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        // Publiserer PasswordChangedEvent
        await PublishPasswordChangedEventAsync(user);

        return Ok(new { Message = "Passordet ditt er tilbakestilt. Du kan nå logge inn med ditt nye passord." });
    }

    // --- 11. SLETT KONTO (Innlogget) ---
    // URL: DELETE /api/auth/account/me
    [HttpDelete("me")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> DeleteAccount()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        // Beskyttelse mot sletting av systemadministrator
        if (await userManager.IsInRoleAsync(user, "Admin"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Systemadministrator kan ikke slettes via API-et." });
        }

        // Henter nødvendig informasjon FØR sletting for å sende med i eventen
        var userId = user.Id;
        var email = user.Email ?? string.Empty;
        var name = GetFullName(user);
        var deletedAt = DateTime.UtcNow;

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        // Publiserer event til MassTransit
        await publishEndpoint.Publish(new UserAccountDeletedByUserEvent
        {
            UserId = userId,
            Email = email,
            Name = name,
            DeletedAt = deletedAt
        });

        return Ok(new { Message = "Kontoen din er slettet." });
    }

    // --- 12. START EKSTERN INNLOGGING (Google Challenge) ---
    // URL: GET /api/auth/account/external-login
    [HttpGet("external-login")]
    public IActionResult ExternalLogin([FromQuery] string provider = "Google")
    {
        if (string.IsNullOrWhiteSpace(provider) || provider.Contains('/'))
        {
            provider = "Google";
        }

        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account");
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

        return Challenge(properties, provider);
    }

    // --- 13. CALLBACK FRA GOOGLE OAUTH ---
    // URL: GET /api/auth/account/external-login-callback
    [HttpGet("external-login-callback")]
    public async Task<IActionResult> ExternalLoginCallback([FromQuery] string? remoteError = null)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        var command = new ProcessGoogleCallbackCommand(info, remoteError);

        var result = await mediator.Send(command);

        return Redirect(result.RedirectUrl!);
    }

    // --- HJELPEMETODER ---
    private async Task PublishPasswordChangedEventAsync(ApplicationUser user)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var deviceInfo = Request.Headers.UserAgent.ToString();

        await publishEndpoint.Publish(new PasswordChangedEvent
        {
            UserId = user.Id,
            Name = GetFullName(user),
            Email = user.Email ?? string.Empty,
            ChangedAt = DateTime.UtcNow,
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress,
            DeviceInfo = string.IsNullOrWhiteSpace(deviceInfo) ? null : deviceInfo
        });
    }

    private static string GetFullName(ApplicationUser user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;
    }

    private async Task<UserProfileResponse> MapToUserProfileResponseAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var isLocked = await userManager.IsLockedOutAsync(user);

        var hasPassword = await userManager.HasPasswordAsync(user);
        var logins = await userManager.GetLoginsAsync(user);
        var isGoogleAccount = logins.Any(l => l.LoginProvider == "Google");

        return new UserProfileResponse
        {
            UserId = user.Id.ToString(),
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = roles.FirstOrDefault() ?? "user",
            HasPassword = hasPassword,
            IsGoogleAccount = isGoogleAccount,
            IsEmailConfirmed = user.EmailConfirmed,
            WelcomeCompleted = user.WelcomeCompleted,
            IsLocked = isLocked,
            CreatedAt = user.CreatedAt,
            LastModifiedAt = user.LastModifiedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                           ?? User.FindFirstValue(OpenIddictConstants.Claims.Subject)
                           ?? Request.Headers["X-User-Id"].FirstOrDefault();

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return null;
        }

        return await userManager.FindByIdAsync(userId.ToString());
    }
}