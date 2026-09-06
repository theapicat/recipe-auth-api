using Contracts.Events.AdminActions;
using Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Admin.Commands.UnlockUser;

public class UnlockUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<UnlockUserCommand, UnlockUserResult>
{
    public async Task<UnlockUserResult> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return UnlockUserResult.NotFound();

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);

        user.LockoutReason = LockoutReason.None;
        user.LockoutReasonDetails = null;
        user.LastModifiedAt = DateTime.UtcNow;

        await userManager.UpdateAsync(user);

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;

        await publishEndpoint.Publish(new UserUnlockedByAdminEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = displayName,
            UnlockedAt = DateTime.UtcNow
        }, cancellationToken);

        return UnlockUserResult.Success(user.Email!);
    }
}