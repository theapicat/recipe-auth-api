using MediatR;

namespace Application.Mediator.Admin.Commands.DeleteAndBlacklistUser;

public record DeleteAndBlacklistUserCommand(
    string UserId,
    string? Reason,
    Guid CurrentAdminId
) : IRequest<DeleteAndBlacklistUserResult>;