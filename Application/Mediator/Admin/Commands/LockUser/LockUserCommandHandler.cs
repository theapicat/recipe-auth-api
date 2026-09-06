using Contracts.Events.AdminActions;
using Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Admin.Commands.UserLockCommand;

public class LockUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<LockUserCommand, LockUserResult>
{
    public async Task<LockUserResult> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.UserId, out var targetGuid))
            return LockUserResult.BadRequest("Ugyldig bruker-ID oppgitt.");

        if (request.CurrentAdminId == targetGuid)
            return LockUserResult.BadRequest("Du kan ikke låse din egen administratorkonto.");

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return LockUserResult.NotFound();

        var reasonDetails = request.ReasonDetails ?? "Kontoen ble sperret av en administrator.";

        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        user.LockoutReason = LockoutReason.ManualAdminLock;
        user.LockoutReasonDetails = reasonDetails;
        user.LastModifiedAt = DateTime.UtcNow;

        await userManager.UpdateAsync(user);

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;

        await publishEndpoint.Publish(new UserLockedByAdminEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = displayName,
            ReasonDetails = reasonDetails,
            LockedAt = DateTime.UtcNow
        }, cancellationToken);

        return LockUserResult.Success(user.Email!);
    }
}