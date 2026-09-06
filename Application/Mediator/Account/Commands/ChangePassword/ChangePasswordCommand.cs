using MediatR;

namespace Application.Mediator.Account.Commands.ChangePassword;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string? IpAddress,
    string? DeviceInfo
) : IRequest<ChangePasswordResult>;