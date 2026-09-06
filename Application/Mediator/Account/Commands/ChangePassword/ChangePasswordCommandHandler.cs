using Contracts.Events.UserActions;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Account.Commands.ChangePassword;

public class ChangePasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<ChangePasswordCommand, ChangePasswordResult>
{
    public async Task<ChangePasswordResult> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            return ChangePasswordResult.Failure("Bruker ikke funnet.");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return ChangePasswordResult.Failure(result.Errors);

        user.LastModifiedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        // Publiserer PasswordChangedEvent
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;

        await publishEndpoint.Publish(new PasswordChangedEvent
        {
            UserId = user.Id,
            Name = displayName,
            Email = user.Email ?? string.Empty,
            ChangedAt = DateTime.UtcNow,
            IpAddress = request.IpAddress,
            DeviceInfo = request.DeviceInfo
        }, cancellationToken);

        return ChangePasswordResult.Success();
    }
}