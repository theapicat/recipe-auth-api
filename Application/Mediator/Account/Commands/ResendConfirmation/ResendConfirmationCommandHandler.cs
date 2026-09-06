using Contracts.Events.UserActions;
using Domain.Options;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Persistence.Context;

namespace Application.Mediator.Account.Commands.ResendConfirmation;

public class ResendConfirmationCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint,
    IOptions<AppSettings> appSettings)
    : IRequestHandler<ResendConfirmationCommand, ResendConfirmationResult>
{
    public async Task<ResendConfirmationResult> Handle(ResendConfirmationCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            return ResendConfirmationResult.Failure("Bruker ikke funnet.");

        if (user.EmailConfirmed)
            return ResendConfirmationResult.AlreadyConfirmed();

        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

        var baseUrl = appSettings.Value.FrontendUrl.TrimEnd('/');
        var confirmationLink =
            $"{baseUrl}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(confirmationToken)}";

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;

        await publishEndpoint.Publish(new ResendEmailConfirmationRequestedEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = displayName,
            ConfirmationLink = confirmationLink,
            RequestedAt = DateTime.UtcNow
        }, cancellationToken);

        return ResendConfirmationResult.Success();
    }
}