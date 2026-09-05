using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Auth.GoogleCallback;

public record ProcessGoogleCallbackCommand(ExternalLoginInfo? ExternalLoginInfo, string? RemoteError) 
    : IRequest<ProcessGoogleCallbackResult>;