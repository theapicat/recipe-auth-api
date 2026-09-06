using Contracts.Events.UserActions;
using Domain.DTOs.Account;
using Domain.DTOs.Account.Responses;
using Domain.Enums;
using Domain.Options;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Persistence.Context;

namespace Application.Mediator.Account.Commands.Register;

public class RegisterUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IPublishEndpoint publishEndpoint,
    IOptions<AppSettings> appSettings) : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 0. Sjekk om e-post eller domene er svartelistet
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailDomain = normalizedEmail.Split('@').LastOrDefault();

        var isBlacklisted = await dbContext.BlacklistedEntries
            .AsNoTracking()
            .AnyAsync(b =>
                    (b.Type == BlacklistType.ExactEmail && b.Pattern.ToLower() == normalizedEmail) ||
                    (b.Type == BlacklistType.Domain && b.Pattern.ToLower() == emailDomain),
                cancellationToken);

        if (isBlacklisted)
            return RegisterUserResult.Failure(
                "Registrering med denne e-postadressen eller e-postleverandøren er ikke tillatt.");

        // 1. Sjekk om e-post finnes fra før
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return RegisterUserResult.Failure("E-postadressen er allerede i bruk.");

        // 2. Opprett ny bruker
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            WelcomeCompleted = false,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return RegisterUserResult.Failure(result.Errors);

        await userManager.AddToRoleAsync(user, "user");

        // 3. Generer bekreftelses-token og bygg bekreftelseslenke
        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

        var baseUrl = appSettings.Value.FrontendUrl.TrimEnd('/');
        var confirmationLink =
            $"{baseUrl}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(confirmationToken)}";

        // 4. Navne-fallback og publisering til RabbitMQ
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName;

        await publishEndpoint.Publish(new UserRegisteredEvent
        {
            UserId = user.Id,
            Name = displayName,
            Email = user.Email,
            ConfirmationLink = confirmationLink,
            RegisteredAt = user.CreatedAt
        }, cancellationToken);

        // 5. Bygg profil-response
        var roles = await userManager.GetRolesAsync(user);
        var userProfile = new UserProfileResponse
        {
            UserId = user.Id.ToString(),
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = roles.FirstOrDefault() ?? "user",
            HasPassword = true,
            IsGoogleAccount = false,
            IsEmailConfirmed = user.EmailConfirmed,
            WelcomeCompleted = user.WelcomeCompleted,
            IsLocked = false,
            CreatedAt = user.CreatedAt,
            LastModifiedAt = user.LastModifiedAt,
            LastLoginAt = user.LastLoginAt
        };

        return RegisterUserResult.Success(userProfile);
    }
}