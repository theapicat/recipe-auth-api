using System.Security.Claims;
using Application.Mediator.Admin.Commands.AdminResendConfirmation;
using Application.Mediator.Admin.Commands.DeleteUserAdmin;
using Application.Mediator.Admin.Commands.ManuallyConfirmEmail;
using Application.Mediator.Admin.Commands.SendPasswordReset;
using Application.Mediator.Admin.Commands.UnlockUser;
using Application.Mediator.Admin.Commands.UpdateUser;
using Application.Mediator.Admin.Commands.UserLockCommand;
using Application.Mediator.Admin.Queries.GetUserDetails;
using Application.Mediator.Admin.Queries.GetUsers;
using Domain.DTOs.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace API.Controllers;

[ApiController]
[Route("api/auth/admin")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, Roles = "Admin")]
public class AdminController(IMediator mediator) : ControllerBase
{
    // --- 1. HENT BRUKERLISTE ---
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await mediator.Send(new GetUsersQuery());
        return Ok(users);
    }

    // --- 2. HENT DETALJERT BRUKERPROFIL FOR ADMIN ---
    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUserDetails(Guid id)
    {
        var result = await mediator.Send(new GetUserDetailsQuery(id));

        if (!result.IsSuccess)
            return NotFound(new { Message = result.ErrorMessage });

        return Ok(result.Details);
    }

    // --- 3. REDIGER BRUKERPERSONALIA ---
    [HttpPut("users")]
    [Consumes("application/json")]
    public async Task<IActionResult> UpdateUser([FromBody] AdminUpdateUserRequest request)
    {
        var command = new AdminUpdateUserCommand(
            request.UserId,
            request.Email,
            request.FirstName,
            request.LastName
        );

        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.IsNotFound)
                return NotFound(new { Message = result.ErrorMessage });

            return BadRequest(result.Errors);
        }

        return Ok(new { Message = "Brukerinformasjonen ble oppdatert." });
    }

    // --- 4. SPERR BRUKER MANUELT (Lockout) ---
    [HttpPost("users/lock")]
    [Consumes("application/json")]
    public async Task<IActionResult> LockUser([FromBody] LockUserRequest request)
    {
        var currentAdminId = GetCurrentAdminId();
        if (currentAdminId == null)
            return Unauthorized(new { Message = "Ugyldig eller manglende administratortoken." });

        var command = new LockUserCommand(request.UserId, request.ReasonDetails, currentAdminId.Value);
        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.IsBadRequest)
                return BadRequest(new { Message = result.ErrorMessage });

            return NotFound(new { Message = result.ErrorMessage });
        }

        return Ok(new { Message = $"Kontoen til {result.TargetEmail} har blitt sperret." });
    }

    // --- 5. GJENÅPNE SPERRET BRUKER (Unlock) ---
    [HttpPost("users/unlock")]
    [Consumes("application/json")]
    public async Task<IActionResult> UnlockUser([FromBody] UnlockUserRequest request)
    {
        var command = new UnlockUserCommand(request.UserId);
        var result = await mediator.Send(command);

        if (!result.IsSuccess)
            return NotFound(new { Message = result.ErrorMessage });

        return Ok(new { Message = $"Sperren for {result.TargetEmail} har blitt fjernet." });
    }

    // --- 6. MANUELL BEKREFTELSE AV E-POST ---
    [HttpPost("users/confirm-email")]
    [Consumes("application/json")]
    public async Task<IActionResult> ManuallyConfirmEmail([FromBody] ResendConfirmationAdminRequest request)
    {
        var command = new ManuallyConfirmEmailCommand(request.UserId);
        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.IsAlreadyConfirmed)
                return BadRequest(new { Message = result.ErrorMessage });

            return NotFound(new { Message = result.ErrorMessage });
        }

        return Ok(new { Message = $"E-postadressen til {result.TargetEmail} er nå manuelt bekreftet." });
    }

    // --- 7. SEND BEKREFTELSESE-POST PÅ VEGNE AV BRUKER ---
    [HttpPost("users/resend-confirmation")]
    [Consumes("application/json")]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationAdminRequest request)
    {
        var command = new AdminResendConfirmationCommand(request.UserId);
        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.IsAlreadyConfirmed)
                return BadRequest(new { Message = result.ErrorMessage });

            return NotFound(new { Message = result.ErrorMessage });
        }

        return Ok(new { Message = $"Ny bekreftelseslenke har blitt sendt til {result.TargetEmail}." });
    }

    // --- 8. SEND PASSORD-TILBAKESTILLING PÅ VEGNE AV BRUKER ---
    [HttpPost("users/reset-password-request")]
    [Consumes("application/json")]
    public async Task<IActionResult> SendPasswordReset([FromBody] ResetPasswordAdminRequest request)
    {
        var command = new AdminSendPasswordResetCommand(request.UserId);
        var result = await mediator.Send(command);

        if (!result.IsSuccess)
            return NotFound(new { Message = result.ErrorMessage });

        return Ok(new { Message = $"Lenke for tilbakestilling av passord har blitt sendt til {result.TargetEmail}." });
    }

    // --- 9. SLETT BRUKER (Admin-sletting) ---
    [HttpPost("users/delete")]
    [Consumes("application/json")]
    public async Task<IActionResult> DeleteUser([FromBody] DeleteUserAdminRequest request)
    {
        var currentAdminId = GetCurrentAdminId();
        if (currentAdminId == null)
            return Unauthorized(new { Message = "Ugyldig eller manglende administratortoken." });

        var command = new DeleteUserAdminCommand(request.UserId, currentAdminId.Value);
        var result = await mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.IsBadRequest)
                return BadRequest(new { Message = result.ErrorMessage });

            if (result.Errors != null)
                return BadRequest(result.Errors);

            return NotFound(new { Message = result.ErrorMessage });
        }

        return Ok(new { Message = $"Bruker {result.TargetEmail} har blitt permanent slettet." });
    }

    // --- HJELPEMETODE ---
    private Guid? GetCurrentAdminId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? User.FindFirstValue(OpenIddictConstants.Claims.Subject);

        return Guid.TryParse(userIdString, out var userId) ? userId : null;
    }
}