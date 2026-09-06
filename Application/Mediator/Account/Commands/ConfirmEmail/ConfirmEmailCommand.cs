using MediatR;

namespace Application.Mediator.Account.Commands.ConfirmEmail;

public record ConfirmEmailCommand(string UserId, string Token) : IRequest<ConfirmEmailResult>;