using Contracts.Events;
using Domain.DTOs;
using Domain.Options;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.Context;

namespace Application.Mediator.Auth.Register;

public class RegisterUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint,
    IOptions<AppSettings> appSettings,
    ILogger<RegisterUserCommandHandler> logger) : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("1. Starter registrering for e-post: {Email}", request.Email);

        // 1. Sjekk om e-post finnes fra før
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            logger.LogWarning("Registrering avbrutt: E-post {Email} er allerede i bruk.", request.Email);
            return new RegisterUserResult(false, ErrorMessage: "E-postadressen er allerede i bruk.");
        }

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

        logger.LogInformation("2. Prøver å opprette bruker i databasen via Identity...");
        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Identity-feil ved opprettelse av bruker {Email}: {Errors}", request.Email, errors);
            return new RegisterUserResult(false, Errors: result.Errors);
        }

        logger.LogInformation("3. Bruker {UserId} opprettet i databasen. Legger til i rolle 'user'...", user.Id);
        await userManager.AddToRoleAsync(user, "user");

        // 3. Generer bekreftelses-token og bygg bekreftelseslenke fra sterk typet konfigurasjon
        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        
        var baseUrl = appSettings.Value.FrontendUrl;
        var confirmationLink = $"{baseUrl}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(confirmationToken)}";

        // 4. Publiser hendelsen til RabbitMQ (Sendes til recipe-notification-service)
        logger.LogInformation("4. Publiserer UserRegisteredEvent til RabbitMQ via MassTransit for {Email}...", user.Email);

        await publishEndpoint.Publish(new UserRegisteredEvent
        {
            UserId = user.Id,
            Name = $"{user.FirstName} {user.LastName}".Trim(),
            Email = user.Email,
            ConfirmationLink = confirmationLink,
            RegisteredAt = user.CreatedAt
        }, cancellationToken);

        logger.LogInformation("5. UserRegisteredEvent ble publisert til RabbitMQ med hell for {Email}!", user.Email);

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

        return new RegisterUserResult(true, UserProfile: userProfile);
    }
}