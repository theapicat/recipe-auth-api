using System.Security.Claims;
using Application.Mediator.Account.Commands.ChangePassword;
using Application.Mediator.Account.Commands.CompleteWelcome;
using Application.Mediator.Account.Commands.ConfirmEmail;
using Application.Mediator.Account.Commands.DeleteAccount;
using Application.Mediator.Account.Commands.GoogleCallback;
using Application.Mediator.Account.Commands.RecoverPassword;
using Application.Mediator.Account.Commands.Register;
using Application.Mediator.Account.Commands.ResendConfirmation;
using Application.Mediator.Account.Commands.ResetPassword;
using Application.Mediator.Account.Commands.SetPassword;
using Application.Mediator.Account.Commands.UpdateProfile;
using Application.Mediator.Account.Queries.GetUserProfile;
using Contracts.Events.UserActions;
using Domain.DTOs;
using Domain.DTOs.Account;
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
                return BadRequest(result.Errors);
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
        if (user == null) 
            return Unauthorized(new { Message = "Ugyldig eller manglende brukertoken." });

        var result = await mediator.Send(new GetUserProfileQuery(user.Id));

        if (!result.IsSuccess)
            return NotFound(new { Message = result.ErrorMessage });

        return Ok(result.UserProfile);
    }

    // --- 3. OPPDATER PROFIL (Innlogget) ---
    // URL: PUT /api/auth/account/profile
    [HttpPut("profile")]
    [Consumes("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { Message = "Ugyldig eller manglende brukertoken." });

        var command = new UpdateProfileCommand(userId.Value, request.FirstName, request.LastName);
        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.IsForbidden)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = result.ErrorMessage });

            if (result.Errors != null)
                return BadRequest(result.Errors);

            return BadRequest(new { Message = result.ErrorMessage });
        }

        return Ok(result.UserProfile);
    }

    // --- 4. BYTT PASSORD (Innlogget - Brukere med eksisterende passord) ---
    // URL: POST /api/auth/account/change-password
    [HttpPost("change-password")]
    [Consumes("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { Message = "Ugyldig eller manglende brukertoken." });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var deviceInfo = Request.Headers.UserAgent.ToString();

        var command = new ChangePasswordCommand(
            userId.Value,
            request.CurrentPassword,
            request.NewPassword,
            string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress,
            string.IsNullOrWhiteSpace(deviceInfo) ? null : deviceInfo
        );

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Errors != null)
                return BadRequest(result.Errors);

            return BadRequest(new { Message = result.ErrorMessage });
        }

        return Ok(new { Message = "Passord ble endret med hell." });
    }

    // --- 5. OPPRETT PASSORD (Innlogget - For Google-brukere uten lokalt passord) ---
    // URL: POST /api/auth/account/set-password
    [HttpPost("set-password")]
    [Consumes("application/json")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { Message = "Ugyldig eller manglende brukertoken." });

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var deviceInfo = Request.Headers.UserAgent.ToString();

        var command = new SetPasswordCommand(
            userId.Value,
            request.NewPassword,
            string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress,
            string.IsNullOrWhiteSpace(deviceInfo) ? null : deviceInfo
        );

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Errors != null)
                return BadRequest(result.Errors);

            return BadRequest(new { Message = result.ErrorMessage });
        }

        return Ok(new { Message = "Passord har blitt opprettet for din konto." });
    }

    // --- 6. FULLFØR VELKOMST (Innlogget) ---
    // URL: GET /api/auth/account/complete-welcome
    [HttpGet("complete-welcome")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> CompleteWelcome()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { Message = "Ugyldig eller manglende brukertoken." });

        var result = await mediator.Send(new CompleteWelcomeCommand(userId.Value));

        if (!result.IsSuccess)
        {
            if (result.Errors != null)
                return BadRequest(result.Errors);

            return NotFound(new { Message = result.ErrorMessage });
        }

        return Ok(result.UserProfile);
    }

    // --- 7. SEND BEKREFTELSESE-POST PÅ NYTT (Innlogget) ---
    // URL: POST /api/auth/account/resend-confirmation
    [HttpPost("resend-confirmation")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> ResendConfirmation()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { Message = "Ugyldig eller manglende brukertoken." });

        var result = await mediator.Send(new ResendConfirmationCommand(userId.Value));

        if (!result.IsSuccess)
        {
            if (result.IsAlreadyConfirmed)
                return BadRequest(new { Message = result.ErrorMessage });

            return NotFound(new { Message = result.ErrorMessage });
        }

        return Ok(new { Message = "Ny bekreftelseslenke har blitt sendt til din e-postadresse." });
    }

    // --- 8. BEKREFT E-POST (Anonym - Brukes fra lenken i e-posten) ---
    // URL: POST /api/auth/account/confirm-email
    [HttpPost("confirm-email")]
    [Consumes("application/json")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        var command = new ConfirmEmailCommand(request.UserId, request.Token);
        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.IsNotFound)
                return NotFound(new { Message = result.ErrorMessage });

            return BadRequest(new { Message = result.ErrorMessage, Errors = result.Errors });
        }

        return Ok(new { Message = "E-postadressen din er bekreftet!" });
    }

    // --- 9. RECOVER / GLEMT PASSORD (Anonym) ---
    // URL: POST /api/auth/account/recover
    [HttpPost("recover")]
    [Consumes("application/json")]
    public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordRequest request)
    {
        var command = new RecoverPasswordCommand(request.Email);
        var result = await mediator.Send(command);

        return Ok(new { Message = result.Message });
    }

    // --- 10. TILBAKESTILL PASSORD (Anonym - Brukes fra lenken i e-posten) ---
    // URL: POST /api/auth/account/reset-password
    [HttpPost("reset-password")]
    [Consumes("application/json")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var deviceInfo = Request.Headers.UserAgent.ToString();

        var command = new ResetPasswordCommand(
            request.Email,
            request.Token,
            request.NewPassword,
            string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress,
            string.IsNullOrWhiteSpace(deviceInfo) ? null : deviceInfo
        );

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.IsNotFound)
                return NotFound(new { Message = result.ErrorMessage });

            return BadRequest(new { Message = result.ErrorMessage, Errors = result.Errors });
        }

        return Ok(new { Message = "Passordet ditt er tilbakestilt. Du kan nå logge inn med ditt nye passord." });
    }

    // --- 11. SLETT KONTO (Innlogget) ---
    // URL: DELETE /api/auth/account/me
    [HttpDelete("me")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(new { Message = "Ugyldig eller manglende brukertoken." });

        var result = await mediator.Send(new DeleteAccountCommand(userId.Value));

        if (!result.IsSuccess)
        {
            if (result.IsForbidden)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = result.ErrorMessage });

            if (result.Errors != null)
                return BadRequest(result.Errors);

            return NotFound(new { Message = result.ErrorMessage });
        }

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


    // DEV :: depicate me please
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
    
    private Guid? GetCurrentUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                           ?? User.FindFirstValue(OpenIddictConstants.Claims.Subject)
                           ?? Request.Headers["X-User-Id"].FirstOrDefault();

        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
}