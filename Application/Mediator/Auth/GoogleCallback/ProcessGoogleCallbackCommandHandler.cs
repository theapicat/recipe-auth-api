using System.Security.Claims;
using Application.TokenService.Interfaces;
using Contracts.Events;
using Domain.Options;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.Context;

namespace Application.Mediator.Auth.GoogleCallback;

public class ProcessGoogleCallbackCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IPublishEndpoint publishEndpoint,
    IOptions<AppSettings> appSettings,
    ITokenService tokenService,
    ILogger<ProcessGoogleCallbackCommandHandler> logger) 
    : IRequestHandler<ProcessGoogleCallbackCommand, ProcessGoogleCallbackResult>
{
    public async Task<ProcessGoogleCallbackResult> Handle(ProcessGoogleCallbackCommand request, CancellationToken cancellationToken)
    {
        var frontendUrl = appSettings.Value.FrontendUrl.TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(request.RemoteError))
        {
            logger.LogWarning("Google OAuth feilet med feilmelding: {Error}", request.RemoteError);
            return new ProcessGoogleCallbackResult(false, RedirectUrl: $"{frontendUrl}/login?error={Uri.EscapeDataString(request.RemoteError)}");
        }

        var info = request.ExternalLoginInfo;
        if (info is null)
        {
            logger.LogWarning("Kunne ikke hente ExternalLoginInfo fra Google.");
            return new ProcessGoogleCallbackResult(false, RedirectUrl: $"{frontendUrl}/login?error=Kunne+ikke+hente+Google-profil");
        }

        // Sjekk om Google-kontoen allerede er koblet til en eksisterende bruker
        var signInResult = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        ApplicationUser? user = null;

        if (signInResult.Succeeded)
        {
            user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        }
        else
        {
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogWarning("E-postadresse mangler fra Google-profilen.");
                return new ProcessGoogleCallbackResult(false, RedirectUrl: $"{frontendUrl}/login?error=Epost+mangler+fra+Google");
            }

            user = await userManager.FindByEmailAsync(email);

            if (user is not null)
            {
                // Brukeren finnes fra før. Knytt Google til kontoen.
                logger.LogInformation("Knytter Google-login til eksisterende bruker {UserId}.", user.Id);
                await userManager.AddLoginAsync(user, info);
            }
            else
            {
                // Brukeren er helt ny. Opprett ny konto med verifisert e-post.
                var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "Google";
                var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "Bruker";
                var now = DateTime.UtcNow;

                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true,
                    CreatedAt = now,
                    LastModifiedAt = now
                };

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    logger.LogError("Identity-feil ved opprettelse av Google-bruker {Email}: {Errors}", email, errors);
                    return new ProcessGoogleCallbackResult(false, RedirectUrl: $"{frontendUrl}/login?error=Kunne+ikke+opprette+bruker");
                }

                await userManager.AddLoginAsync(user, info);
                await userManager.AddToRoleAsync(user, "user");

                logger.LogInformation("Ny bruker {UserId} opprettet via Google OAuth. Publiserer UserRegisteredWithGoogleEvent til MassTransit...", user.Id);

                await publishEndpoint.Publish(new UserRegisteredWithGoogleEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Name = $"{user.FirstName} {user.LastName}".Trim(),
                    RegisteredAt = now
                }, cancellationToken);
            }
        }

        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            logger.LogWarning("Bruker {UserId} er sperret eller ble ikke funnet.", user?.Id);
            return new ProcessGoogleCallbackResult(false, RedirectUrl: $"{frontendUrl}/login?error=Kontoen+er+sperret");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var hasPassword = await userManager.HasPasswordAsync(user);

        // Generer ekte JWT token via ITokenService
        var accessToken = await tokenService.GenerateAccessTokenAsync(user);
        var refreshToken = Guid.NewGuid().ToString("N");

        // Bygg callback-URL med gyldig JWT token
        var callbackUrl = $"{frontendUrl}/api/auth/google-callback" +
            $"?access_token={Uri.EscapeDataString(accessToken)}" +
            $"&refresh_token={refreshToken}" +
            $"&user_id={user.Id}" +
            $"&email={Uri.EscapeDataString(user.Email!)}" +
            $"&first_name={Uri.EscapeDataString(user.FirstName)}" +
            $"&last_name={Uri.EscapeDataString(user.LastName)}" +
            $"&role={Uri.EscapeDataString(roles.FirstOrDefault() ?? "user")}" +
            $"&has_password={hasPassword.ToString().ToLower()}" +
            $"&welcome_completed={user.WelcomeCompleted.ToString().ToLower()}";

        return new ProcessGoogleCallbackResult(true, RedirectUrl: callbackUrl);
    }
}