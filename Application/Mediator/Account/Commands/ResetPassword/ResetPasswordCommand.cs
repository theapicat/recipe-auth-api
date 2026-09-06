using MediatR;

namespace Application.Mediator.Account.Commands.ResetPassword;

public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword,
    string? IpAddress,
    string? DeviceInfo
) : IRequest<ResetPasswordResult>;