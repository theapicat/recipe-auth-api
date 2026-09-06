using Contracts.Events.AdminActions;
using Domain.Entities;
using Domain.Enums;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Admin.Commands.DeleteAndBlacklistUser;

public class DeleteAndBlacklistUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<DeleteAndBlacklistUserCommand, DeleteAndBlacklistUserResult>
{
    public async Task<DeleteAndBlacklistUserResult> Handle(DeleteAndBlacklistUserCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.UserId, out var targetGuid))
            return DeleteAndBlacklistUserResult.BadRequest("Ugyldig bruker-ID oppgitt.");

        if (request.CurrentAdminId == targetGuid)
            return DeleteAndBlacklistUserResult.BadRequest("Du kan ikke slette/svarteliste din egen administratorkonto.");

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return DeleteAndBlacklistUserResult.NotFound();

        var userId = user.Id;
        var email = user.Email ?? string.Empty;
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;

        // 1. Legg e-posten inn i svartelisten
        var blacklistEntry = new BlacklistedEntry
        {
            Pattern = normalizedEmail,
            Type = BlacklistType.ExactEmail,
            Reason = request.Reason ?? "Slettet og svartelistet av administrator pga. brudd på brukervilkår.",
            CreatedByAdminId = request.CurrentAdminId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.BlacklistedEntries.Add(blacklistEntry);

        // 2. Slett brukeren fra databasen
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return DeleteAndBlacklistUserResult.Failure(result.Errors);

        await dbContext.SaveChangesAsync(cancellationToken);

        // 3. Publiser kombinert slett- og svartelistingsevent
        await publishEndpoint.Publish(new UserDeletedAndBlacklistedByAdminEvent
        {
            UserId = userId,
            Email = email,
            Name = displayName,
            Reason = blacklistEntry.Reason,
            DeletedAt = DateTime.UtcNow
        }, cancellationToken);

        return DeleteAndBlacklistUserResult.Success(email);
    }
}