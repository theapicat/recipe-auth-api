using MediatR;

namespace Application.Mediator.Admin.Commands.SendPasswordReset;

public record AdminSendPasswordResetCommand(string UserId) : IRequest<AdminSendPasswordResetResult>;