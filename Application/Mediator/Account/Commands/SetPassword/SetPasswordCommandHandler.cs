using Contracts.Events.UserActions;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Account.Commands.SetPassword;

public class SetPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<SetPasswordCommand, SetPasswordResult>
{
    public async Task<SetPasswordResult> Handle(SetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            return SetPasswordResult.Failure("Bruker ikke funnet.");

        var hasPassword = await userManager.HasPasswordAsync(user);
        if (hasPassword)
            return SetPasswordResult.Failure("Kontoen din har allerede et passord. Bruk change-password i stedet.");

        var result = await userManager.AddPasswordAsync(user, request.NewPassword);
        if (!result.Succeeded)
            return SetPasswordResult.Failure(result.Errors);

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

        return SetPasswordResult.Success();
    }
}