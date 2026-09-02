using MediatR;

namespace Application.Mediator.Register;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<RegisterUserResult>;