using MediatR;

namespace Application.Mediator.Admin.Commands.UserLockCommand;

public record LockUserCommand(
    string UserId,
    string? ReasonDetails,
    Guid CurrentAdminId
) : IRequest<LockUserResult>;