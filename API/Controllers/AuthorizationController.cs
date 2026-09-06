using Application.Mediator.Authorization.Commands.PasswordGrant;
using Application.Mediator.Authorization.Commands.RefreshTokenGrant;
using MediatR;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace API.Controllers;

[ApiController]
[Route("api/auth/connect")]
public class AuthorizationController(IMediator mediator) : ControllerBase
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
            var command = new PasswordGrantCommand(request.Username ?? string.Empty, request.Password ?? string.Empty);
            var result = await mediator.Send(command);

            if (!result.IsSuccess)
            {
                return ChallengeWithError(result.ErrorDescription!);
            }

            return SignIn(result.Principal!, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // ----------------------------------------------------
        // 2. Refresh Token Grant (Automatisk token-fornyelse)
        // ----------------------------------------------------
        if (request.IsRefreshTokenGrantType())
        {
            var authResult = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            var command = new RefreshTokenGrantCommand(authResult.Principal);
            var result = await mediator.Send(command);

            if (!result.IsSuccess)
            {
                return ChallengeWithError(result.ErrorDescription!);
            }

            return SignIn(result.Principal!, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
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