using MediatR;

namespace Application.Mediator.Account.Commands.RemoveBlacklistEntry;

public record RemoveBlacklistEntryCommand(Guid EntryId) : IRequest<bool>;