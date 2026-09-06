using MediatR;

namespace Application.Mediator.Admin.Commands.LockUser;

public record LockUserCommand(
    string UserId,
    string? ReasonDetails,
    Guid CurrentAdminId
) : IRequest<LockUserResult>;