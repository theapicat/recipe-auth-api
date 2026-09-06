using Contracts.Events.UserActions;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Account.Commands.DeleteAccount;

public class DeleteAccountCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<DeleteAccountCommand, DeleteAccountResult>
{
    public async Task<DeleteAccountResult> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            return DeleteAccountResult.Failure("Bruker ikke funnet.");

        // Beskyttelse mot sletting av systemadministrator
        if (await userManager.IsInRoleAsync(user, "Admin"))
            return DeleteAccountResult.Forbidden("Systemadministrator kan ikke slettes via API-et.");

        var userId = user.Id;
        var email = user.Email ?? string.Empty;
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;
        var deletedAt = DateTime.UtcNow;

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return DeleteAccountResult.Failure(result.Errors);

        // Publiserer event til MassTransit
        await publishEndpoint.Publish(new UserAccountDeletedByUserEvent
        {
            UserId = userId,
            Email = email,
            Name = displayName,
            DeletedAt = deletedAt
        }, cancellationToken);

        return DeleteAccountResult.Success();
    }
}