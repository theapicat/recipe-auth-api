using MediatR;

namespace Application.Mediator.Account.Commands.DeleteAccount;

public record DeleteAccountCommand(Guid UserId) : IRequest<DeleteAccountResult>;