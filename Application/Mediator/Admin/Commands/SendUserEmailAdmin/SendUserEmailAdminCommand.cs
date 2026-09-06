using MediatR;

namespace Application.Mediator.Admin.Commands.SendUserEmailAdmin;

public record SendUserEmailAdminCommand(
    string UserId,
    string Subject,
    string Message,
    Guid CurrentAdminId
) : IRequest<SendUserEmailAdminResult>;