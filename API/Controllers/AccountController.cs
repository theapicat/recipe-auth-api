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
[Route("api/account")]
public class AccountController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    // --- REGISTRERING ---
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
            LastName = request.LastName
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        // Tildel alltid standardrollen "user"
        await userManager.AddToRoleAsync(user, "user");

        var response = await MapToUserProfileResponseAsync(user);
        return Ok(response);
    }

    // --- HENT MIN PROFIL ---
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GetProfile()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        var response = await MapToUserProfileResponseAsync(user);
        return Ok(response);
    }

    // --- OPPDATER PROFIL ---
    [HttpPut("profile")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.AvatarUrl = request.AvatarUrl ?? string.Empty;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        var response = await MapToUserProfileResponseAsync(user);
        return Ok(response);
    }

    // --- BYTT PASSORD ---
    [HttpPost("change-password")]
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

        return Ok(new { Message = "Passord ble endret med hell." });
    }

    // --- SLETT KONTO ---
    [HttpDelete("me")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> DeleteAccount()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return NotFound(new { Message = "Bruker ikke funnet." });

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

        return new UserProfileResponse
        {
            UserId = user.Id.ToString(),
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            Role = roles.FirstOrDefault() ?? "user",
            IsEmailConfirmed = user.EmailConfirmed,
            CreatedAt = user.CreatedAt,
            LastModifiedAt = user.LastModifiedAt
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