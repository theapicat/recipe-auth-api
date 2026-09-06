using Domain.Enums;
using MediatR;

namespace Application.Mediator.Admin.Commands.AddBlacklistEntry;

public record AddBlacklistEntryCommand(
    string Pattern,
    BlacklistType Type,
    string? Reason,
    Guid CurrentAdminId
) : IRequest<AddBlacklistEntryResult>;