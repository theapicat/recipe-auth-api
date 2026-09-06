using Contracts.Events.AdminActions;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Admin.Commands.UpdateUser;

public class AdminUpdateUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<AdminUpdateUserCommand, AdminUpdateUserResult>
{
    public async Task<AdminUpdateUserResult> Handle(AdminUpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return AdminUpdateUserResult.NotFound();

        var oldEmail = user.Email ?? string.Empty;
        var emailChanged = !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;

        if (emailChanged)
        {
            user.Email = request.Email;
            user.UserName = request.Email;
        }

        user.LastModifiedAt = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return AdminUpdateUserResult.Failure(result.Errors);

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;

        await publishEndpoint.Publish(new UserUpdatedByAdminEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = displayName,
            OldEmail = oldEmail,
            NewEmail = request.Email,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken);

        return AdminUpdateUserResult.Success();
    }
}