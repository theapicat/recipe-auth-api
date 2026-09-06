using Contracts.Events.UserActions;
using Domain.Options;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Persistence.Context;

namespace Application.Mediator.Admin.Commands.AdminResendConfirmation;

public class AdminResendConfirmationCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint,
    IOptions<AppSettings> appSettings)
    : IRequestHandler<AdminResendConfirmationCommand, AdminResendConfirmationResult>
{
    public async Task<AdminResendConfirmationResult> Handle(AdminResendConfirmationCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return AdminResendConfirmationResult.NotFound();

        if (user.EmailConfirmed)
            return AdminResendConfirmationResult.AlreadyConfirmed();

        var confirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var baseUrl = appSettings.Value.FrontendUrl.TrimEnd('/');
        var confirmationLink = $"{baseUrl}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(confirmationToken)}";

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

        return AdminResendConfirmationResult.Success(user.Email!);
    }
}