using MediatR;

namespace Application.Mediator.Account.Commands.ResendConfirmation;

public record ResendConfirmationCommand(Guid UserId) : IRequest<ResendConfirmationResult>;