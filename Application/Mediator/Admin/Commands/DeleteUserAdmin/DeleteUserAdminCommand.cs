using MediatR;

namespace Application.Mediator.Admin.Commands.DeleteUserAdmin;

public record DeleteUserAdminCommand(
    string UserId,
    Guid CurrentAdminId
) : IRequest<DeleteUserAdminResult>;