using MediatR;

namespace Application.Mediator.Account.Commands.RecoverPassword;

public record RecoverPasswordCommand(string Email) : IRequest<RecoverPasswordResult>;