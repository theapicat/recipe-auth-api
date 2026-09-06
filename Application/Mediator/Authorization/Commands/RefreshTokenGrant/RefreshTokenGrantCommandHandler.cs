using Application.Mediator.Authorization.Commands.PasswordGrant;
using Application.TokenService.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using Persistence.Context;

namespace Application.Mediator.Authorization.Commands.RefreshTokenGrant;

public class RefreshTokenGrantCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService)
    : IRequestHandler<RefreshTokenGrantCommand, TokenExchangeResult>
{
    public async Task<TokenExchangeResult> Handle(RefreshTokenGrantCommand request, CancellationToken cancellationToken)
    {
        if (request.AuthenticatedPrincipal is null)
        {
            return TokenExchangeResult.Failure("Ugyldig eller utløpt refresh token.");
        }

        // Hent bruker-ID fra eksisterende token-claims
        var userId = request.AuthenticatedPrincipal.GetClaim(OpenIddictConstants.Claims.Subject);
        if (string.IsNullOrEmpty(userId))
        {
            return TokenExchangeResult.Failure("Ugyldig token-identitetsdata.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || await userManager.IsLockedOutAsync(user) || !await signInManager.CanSignInAsync(user))
        {
            return TokenExchangeResult.Failure("Kontoen er sperret eller eksisterer ikke lenger.");
        }

        // Oppdater LastLoginAt ved hver vellykkede token-fornyelse
        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        // Re-opprett ClaimsPrincipal for å sikre at nye navn, roller eller felter blir inkludert
        var freshPrincipal = await tokenService.CreateClaimsPrincipalAsync(user);

        return TokenExchangeResult.Success(freshPrincipal);
    }
}