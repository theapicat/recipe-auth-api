using System.Security.Claims;
using API.Context;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace API.Controllers;

[ApiController]
public class AuthorizationController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
    : ControllerBase
{
    [HttpPost("~/connect/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("Kunne ikke hente OpenID Connect-forespørselen.");

        // 1. Password Grant (Innlogging)
        if (request.IsPasswordGrantType())
        {
            var user = await userManager.FindByEmailAsync(request.Username!) 
                ?? await userManager.FindByNameAsync(request.Username!);

            if (user is null)
            {
                return ChallengeWithError("Ugyldig e-post eller passord.");
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password!, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                return ChallengeWithError("Ugyldig e-post eller passord.");
            }

            var principal = await CreateClaimsPrincipalAsync(user);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // 2. Refresh Token Grant (Fornyelse)
        if (request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return ChallengeWithError("Ugyldig eller utløpt refresh token.");
            }

            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(new { Error = "Ugyldig grant_type angitt." });
    }

    private async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        identity.AddClaim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
        identity.AddClaim(OpenIddictConstants.Claims.Email, user.Email ?? string.Empty);
        identity.AddClaim(OpenIddictConstants.Claims.GivenName, user.FirstName);
        identity.AddClaim(OpenIddictConstants.Claims.FamilyName, user.LastName);

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(OpenIddictConstants.Claims.Role, role);
        }

        identity.SetDestinations(_ => new[] { OpenIddictConstants.Destinations.AccessToken });

        var principal = new ClaimsPrincipal(identity);
        
        // Inkluderer OfflineAccess for å tvinge utstedelse av Refresh Token
        principal.SetScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles,
            OpenIddictConstants.Scopes.OfflineAccess
        );

        return principal;
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