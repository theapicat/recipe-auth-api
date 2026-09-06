using MediatR;

namespace Application.Mediator.Account.Commands.SetPassword;

public record SetPasswordCommand(
    Guid UserId,
    string NewPassword,
    string? IpAddress,
    string? DeviceInfo
) : IRequest<SetPasswordResult>;