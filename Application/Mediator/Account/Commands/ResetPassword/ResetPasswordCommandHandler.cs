using Contracts.Events.UserActions;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Account.Commands.ResetPassword;

public class ResetPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
{
    public async Task<ResetPasswordResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return ResetPasswordResult.NotFound();

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return ResetPasswordResult.Failure(result.Errors);

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

        return ResetPasswordResult.Success();
    }
}