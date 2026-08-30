using System.Security.Claims;
using API.Context;
using API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace API.Controllers;

[ApiController]
[Route("~/account")]
public class AccountController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    // --- 1. REGISTRERING (Anonym) ---
    [HttpPost("register")]
    [Consumes("application/x-www-form-urlencoded", "application/json")]
    public async Task<IActionResult> Register([FromForm] RegisterRequest request)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { Message = "E-postadressen er allerede i bruk." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            WelcomeCompleted = false,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        await userManager.AddToRoleAsync(user, "user");

        // Generer e-postbekreftelsestoken ved registrering
        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

        var response = await MapToUserProfileResponseAsync(user);
        return Ok(response);
    }

    // --- 2. HENT MIN PROFIL (Innlogget) ---
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

        return Ok(new { Message = "Passord ble endret med hell." });
    }

    // --- 5. OPPRETT PASSORD (Innlogget - For Google-brukere uten lokalt passord) ---
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

        return Ok(new { Message = "Passord har blitt opprettet for din konto." });
    }

    // --- 6. FULLFØR VELKOMST (Innlogget) ---
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

        user.LastModifiedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return Ok(new { Message = "E-postadressen din er bekreftet!" });
    }

    // --- 9. RECOVER / GLEMT PASSORD (Anonym) ---
    [HttpPost("recover")]
    [Consumes("application/json")]
    public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Ok(new { Message = "Dersom e-posten er registrert, har instruksjoner om tilbakestilling blitt sendt." });
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        return Ok(new { Message = "Instruksjoner om tilbakestilling er generert.", ResetToken = resetToken });
    }

    // --- 10. TILBAKESTILL PASSORD (Anonym - Brukes fra lenken i e-posten) ---
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

        return Ok(new { Message = "Passordet ditt er tilbakestilt. Du kan nå logge inn med ditt nye passord." });
    }

    // --- 11. SLETT KONTO (Innlogget) ---
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

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { Message = "Kontoen din er slettet." });
    }

    // --- HJELPEMETODER ---
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