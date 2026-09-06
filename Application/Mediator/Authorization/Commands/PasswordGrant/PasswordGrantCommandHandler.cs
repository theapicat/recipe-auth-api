using Application.TokenService.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Authorization.Commands.PasswordGrant;

public class PasswordGrantCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenService tokenService)
    : IRequestHandler<PasswordGrantCommand, TokenExchangeResult>
{
    public async Task<TokenExchangeResult> Handle(PasswordGrantCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Username) 
                   ?? await userManager.FindByNameAsync(request.Username);

        if (user is null)
        {
            return TokenExchangeResult.Failure("Ugyldig e-post eller passord.");
        }

        // Sjekk om kontoen er sperret eller ikke kan logge inn
        if (await userManager.IsLockedOutAsync(user) || !await signInManager.CanSignInAsync(user))
        {
            return TokenExchangeResult.Failure("Kontoen din er sperret eller deaktivert.");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return TokenExchangeResult.Failure("Ugyldig e-post eller passord.");
        }

        // Oppdater LastLoginAt ved vellykket innlogging
        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var principal = await tokenService.CreateClaimsPrincipalAsync(user);
        return TokenExchangeResult.Success(principal);
    }
}