using Domain.Entities;
using MediatR;

namespace Application.Mediator.Account.Queries.GetBlacklistEntry;

public record GetBlacklistEntriesQuery() : IRequest<List<BlacklistedEntry>>;