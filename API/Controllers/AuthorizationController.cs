using System.Security.Claims;
using Application.TokenService.Interfaces;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Persistence.Context;

namespace API.Controllers;

[ApiController]
[Route("api/auth/connect")]
public class AuthorizationController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService)
    : ControllerBase
{
    // --- TOKEN ENDEPUNKT ---
    // URL: POST /api/auth/connect/token
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("Kunne ikke hente OpenID Connect-forespørselen.");

        // ----------------------------------------------------
        // 1. Password Grant (Førstegangs innlogging med e-post + passord)
        // ----------------------------------------------------
        if (request.IsPasswordGrantType())
        {
            var user = await userManager.FindByEmailAsync(request.Username!) 
                ?? await userManager.FindByNameAsync(request.Username!);

            if (user is null)
            {
                return ChallengeWithError("Ugyldig e-post eller passord.");
            }

            // Sjekk om kontoen er sperret eller ikke kan logge inn
            if (await userManager.IsLockedOutAsync(user) || !await signInManager.CanSignInAsync(user))
            {
                return ChallengeWithError("Kontoen din er sperret eller deaktivert.");
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password!, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                return ChallengeWithError("Ugyldig e-post eller passord.");
            }

            // Oppdater LastLoginAt ved vellykket innlogging
            user.LastLoginAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            var principal = await tokenService.CreateClaimsPrincipalAsync(user);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // ----------------------------------------------------
        // 2. Refresh Token Grant (Automatisk token-fornyelse)
        // ----------------------------------------------------
        if (request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal is null)
            {
                return ChallengeWithError("Ugyldig eller utløpt refresh token.");
            }

            // Hent bruker-ID fra eksisterende token-claims
            var userId = result.Principal.GetClaim(OpenIddictConstants.Claims.Subject);
            if (string.IsNullOrEmpty(userId))
            {
                return ChallengeWithError("Ugyldig token-identitetsdata.");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user is null || await userManager.IsLockedOutAsync(user) || !await signInManager.CanSignInAsync(user))
            {
                return ChallengeWithError("Kontoen er sperret eller eksisterer ikke lenger.");
            }

            // Oppdater LastLoginAt ved hver vellykkede token-fornyelse
            user.LastLoginAt = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            // Re-opprett ClaimsPrincipal for å sikre at nye navn, roller eller felter blir inkludert
            var freshPrincipal = await tokenService.CreateClaimsPrincipalAsync(user);

            return SignIn(freshPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new { Error = "Ugyldig grant_type angitt." });
    }

    private IActionResult ChallengeWithError(string description)
    {
        return Challenge(
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }),
            authenticationSchemes: new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
    }
}