using System.Security.Claims;
using Application.TokenService.Interfaces;
using Contracts.Events.UserActions;
using Domain.Enums;
using Domain.Options;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.Context;

namespace Application.Mediator.Account.Commands.GoogleCallback;

public class ProcessGoogleCallbackCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ApplicationDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    IOptions<AppSettings> appSettings,
    ITokenService tokenService)
    : IRequestHandler<ProcessGoogleCallbackCommand, ProcessGoogleCallbackResult>
{
    public async Task<ProcessGoogleCallbackResult> Handle(ProcessGoogleCallbackCommand request,
        CancellationToken cancellationToken)
    {
        var frontendUrl = appSettings.Value.FrontendUrl.TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(request.RemoteError))
            return new ProcessGoogleCallbackResult(false, $"{frontendUrl}/login?error=access_denied");

        var info = request.ExternalLoginInfo;
        if (info is null) return new ProcessGoogleCallbackResult(false, $"{frontendUrl}/login?error=google_failed");

        // Hent e-post fra Google Principal
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return new ProcessGoogleCallbackResult(false, $"{frontendUrl}/login?error=google_failed");

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var emailDomain = normalizedEmail.Split('@').LastOrDefault();

        // 1. SVARTELISTESJEKK (Sjekk om e-post eller domene er svartelistet)
        var isBlacklisted = await dbContext.BlacklistedEntries
            .AsNoTracking()
            .AnyAsync(b =>
                    (b.Type == BlacklistType.ExactEmail && b.Pattern.ToLower() == normalizedEmail) ||
                    (b.Type == BlacklistType.Domain && b.Pattern.ToLower() == emailDomain),
                cancellationToken);

        if (isBlacklisted) return new ProcessGoogleCallbackResult(false, $"{frontendUrl}/login?error=blacklisted");

        // 2. Finn eksisterende bruker (enten via Google LoginInfo eller e-post)
        var user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey)
                   ?? await userManager.FindByEmailAsync(email);

        if (user is not null)
        {
            // Sjekk om den eksisterende brukeren er sperret eller utestengt
            if (await userManager.IsLockedOutAsync(user) || !await signInManager.CanSignInAsync(user))
                return new ProcessGoogleCallbackResult(false, $"{frontendUrl}/login?error=account_locked");

            // Knytt Google-innlogging til brukeren dersom den ikke er knyttet fra før
            var logins = await userManager.GetLoginsAsync(user);
            if (!logins.Any(l => l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey))
                await userManager.AddLoginAsync(user, info);
        }
        else
        {
            // 3. Brukeren finnes ikke fra før -> Opprett ny bruker via Google
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
                return new ProcessGoogleCallbackResult(false, $"{frontendUrl}/login?error=google_failed");

            await userManager.AddLoginAsync(user, info);
            await userManager.AddToRoleAsync(user, "user");

            await publishEndpoint.Publish(new UserRegisteredWithGoogleEvent
            {
                UserId = user.Id,
                Email = user.Email,
                Name = $"{user.FirstName} {user.LastName}".Trim(),
                RegisteredAt = now
            }, cancellationToken);
        }

        // Dobbeltsjekk sperre før token-generering
        if (await userManager.IsLockedOutAsync(user) || !await signInManager.CanSignInAsync(user))
            return new ProcessGoogleCallbackResult(false, $"{frontendUrl}/login?error=account_locked");

        // Oppdater LastLoginAt
        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var hasPassword = await userManager.HasPasswordAsync(user);

        // Generer JWT token
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

        return new ProcessGoogleCallbackResult(true, callbackUrl);
    }
}