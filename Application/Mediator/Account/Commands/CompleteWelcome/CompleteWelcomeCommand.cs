using MediatR;

namespace Application.Mediator.Account.Commands.CompleteWelcome;

public record CompleteWelcomeCommand(Guid UserId) : IRequest<CompleteWelcomeResult>;