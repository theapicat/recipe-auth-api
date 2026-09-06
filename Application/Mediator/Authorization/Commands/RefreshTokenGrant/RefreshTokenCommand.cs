using System.Security.Claims;
using Application.Mediator.Authorization.Commands.PasswordGrant;
using MediatR;

namespace Application.Mediator.Authorization.Commands.RefreshTokenGrant;

public record RefreshTokenGrantCommand(ClaimsPrincipal? AuthenticatedPrincipal) : IRequest<TokenExchangeResult>;