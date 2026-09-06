using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Application.Mediator.Admin.Commands.AddBlacklistEntry;

public class AddBlacklistEntryCommandHandler(ApplicationDbContext dbContext)
    : IRequestHandler<AddBlacklistEntryCommand, AddBlacklistEntryResult>
{
    public async Task<AddBlacklistEntryResult> Handle(AddBlacklistEntryCommand request,
        CancellationToken cancellationToken)
    {
        var cleanedPattern = request.Pattern.Trim().ToLowerInvariant().TrimStart('@');

        if (string.IsNullOrWhiteSpace(cleanedPattern))
            return AddBlacklistEntryResult.Failure("Mønster/E-post kan ikke være tom.");

        var exists = await dbContext.BlacklistedEntries
            .AnyAsync(b => b.Pattern.ToLower() == cleanedPattern && b.Type == request.Type, cancellationToken);

        if (exists)
            return AddBlacklistEntryResult.Failure("Denne e-posten eller dette domenet er allerede svartelistet.");

        var entry = new BlacklistedEntry
        {
            Pattern = cleanedPattern,
            Type = request.Type,
            Reason = request.Reason,
            CreatedByAdminId = request.CurrentAdminId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.BlacklistedEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return AddBlacklistEntryResult.Success();
    }
}