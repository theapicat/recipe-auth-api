using MediatR;

namespace Application.Mediator.Account.Commands.UpdateProfile;

public record UpdateProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName
) : IRequest<UpdateProfileResult>;