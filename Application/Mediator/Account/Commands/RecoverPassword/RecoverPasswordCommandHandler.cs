using Contracts.Events.UserActions;
using Domain.Options;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Persistence.Context;

namespace Application.Mediator.Account.Commands.RecoverPassword;

public class RecoverPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint,
    IOptions<AppSettings> appSettings)
    : IRequestHandler<RecoverPasswordCommand, RecoverPasswordResult>
{
    public async Task<RecoverPasswordResult> Handle(RecoverPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        // Av sikkerhetsgrunner returneres samme respons uavhengig av om brukeren eksisterer
        if (user == null)
        {
            return RecoverPasswordResult.Success();
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

        var baseUrl = appSettings.Value.FrontendUrl.TrimEnd('/');
        var resetLink = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(resetToken)}";

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;

        await publishEndpoint.Publish(new PasswordResetRequestedEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = displayName,
            ResetLink = resetLink,
            RequestedAt = DateTime.UtcNow
        }, cancellationToken);

        return RecoverPasswordResult.Success();
    }
}