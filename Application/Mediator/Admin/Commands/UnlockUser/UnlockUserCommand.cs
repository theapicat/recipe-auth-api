using MediatR;

namespace Application.Mediator.Admin.Commands.UnlockUser;

public record UnlockUserCommand(string UserId) : IRequest<UnlockUserResult>;