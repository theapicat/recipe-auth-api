using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Application.Mediator.Account.Queries.GetBlacklistEntry;

public class GetBlacklistEntriesQueryHandler(ApplicationDbContext dbContext)
    : IRequestHandler<GetBlacklistEntriesQuery, List<BlacklistedEntry>>
{
    public async Task<List<BlacklistedEntry>> Handle(GetBlacklistEntriesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.BlacklistedEntries
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}