using MediatR;
using Persistence.Context;

namespace Application.Mediator.Account.Commands.RemoveBlacklistEntry;

public class RemoveBlacklistEntryCommandHandler(ApplicationDbContext dbContext)
    : IRequestHandler<RemoveBlacklistEntryCommand, bool>
{
    public async Task<bool> Handle(RemoveBlacklistEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await dbContext.BlacklistedEntries.FindAsync([request.EntryId], cancellationToken);
        if (entry == null)
            return false;

        dbContext.BlacklistedEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}