using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.GoogleCallback;

public record ProcessGoogleCallbackCommand(ExternalLoginInfo? ExternalLoginInfo, string? RemoteError, Uri BaseUri)
    : IRequest<ProcessGoogleCallbackResult>;