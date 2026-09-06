using MediatR;

namespace Application.Mediator.Admin.Commands.AdminResendConfirmation;

public record AdminResendConfirmationCommand(string UserId) : IRequest<AdminResendConfirmationResult>;