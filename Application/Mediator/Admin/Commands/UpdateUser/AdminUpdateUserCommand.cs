using MediatR;

namespace Application.Mediator.Admin.Commands.UpdateUser;

public record AdminUpdateUserCommand(
    string UserId,
    string Email,
    string FirstName,
    string LastName
) : IRequest<AdminUpdateUserResult>;