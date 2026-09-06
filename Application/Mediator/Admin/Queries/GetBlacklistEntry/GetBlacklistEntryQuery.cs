using Domain.Entities;
using MediatR;

namespace Application.Mediator.Admin.Queries.GetBlacklistEntry;

public record GetBlacklistEntriesQuery : IRequest<List<BlacklistedEntry>>;