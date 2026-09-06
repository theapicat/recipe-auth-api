using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.GoogleCallback;

public record ProcessGoogleCallbackCommand(ExternalLoginInfo? ExternalLoginInfo, string? RemoteError) 
    : IRequest<ProcessGoogleCallbackResult>;