using MediatR;
using Microsoft.AspNetCore.Identity;

public record ProcessGoogleCallbackCommand(ExternalLoginInfo? ExternalLoginInfo, string? RemoteError) 
    : IRequest<ProcessGoogleCallbackResult>;