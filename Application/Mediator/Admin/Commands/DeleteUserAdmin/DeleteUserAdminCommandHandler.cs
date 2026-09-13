using Contracts.Events.AdminActions;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Admin.Commands.DeleteUserAdmin;

public class DeleteUserAdminCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<DeleteUserAdminCommand, DeleteUserAdminResult>
{
    public async Task<DeleteUserAdminResult> Handle(DeleteUserAdminCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.UserId, out var targetGuid))
            return DeleteUserAdminResult.BadRequest("Ugyldig bruker-ID oppgitt.");

        if (request.CurrentAdminId == targetGuid)
            return DeleteUserAdminResult.BadRequest("Du kan ikke slette din egen administratorkonto.");

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return DeleteUserAdminResult.NotFound();

        if (await userManager.IsInRoleAsync(user, "Admin"))
            return DeleteUserAdminResult.BadRequest("Du kan ikke slette en annen administratorkonto.");

        var userId = user.Id;
        var email = user.Email ?? string.Empty;
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return DeleteUserAdminResult.Failure(result.Errors);

        await publishEndpoint.Publish(new UserAccountDeletedByAdminEvent
        {
            UserId = userId,
            Email = email,
            Name = displayName,
            DeletedAt = DateTime.UtcNow
        }, cancellationToken);

        return DeleteUserAdminResult.Success(email);
    }
}