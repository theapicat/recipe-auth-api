using MediatR;

namespace Application.Mediator.Authorization.Commands.PasswordGrant;

public record PasswordGrantCommand(string Username, string Password) : IRequest<TokenExchangeResult>;