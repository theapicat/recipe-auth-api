using Contracts.Events.AdminActions;
using Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Admin.Commands.ManuallyConfirmEmail;

public class ManuallyConfirmEmailCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<ManuallyConfirmEmailCommand, ManuallyConfirmEmailResult>
{
    public async Task<ManuallyConfirmEmailResult> Handle(ManuallyConfirmEmailCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return ManuallyConfirmEmailResult.NotFound();

        if (user.EmailConfirmed)
            return ManuallyConfirmEmailResult.AlreadyConfirmed();

        user.EmailConfirmed = true;

        if (user.LockoutReason == LockoutReason.UnconfirmedEmail14Days)
        {
            await userManager.SetLockoutEndDateAsync(user, null);
            user.LockoutReason = LockoutReason.None;
            user.LockoutReasonDetails = null;
        }

        user.LastModifiedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;

        await publishEndpoint.Publish(new EmailManuallyConfirmedByAdminEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = displayName,
            ConfirmedAt = DateTime.UtcNow
        }, cancellationToken);

        return ManuallyConfirmEmailResult.Success(user.Email!);
    }
}