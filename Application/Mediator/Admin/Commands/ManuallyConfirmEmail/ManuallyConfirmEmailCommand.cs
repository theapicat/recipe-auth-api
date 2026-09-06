using MediatR;

namespace Application.Mediator.Admin.Commands.ManuallyConfirmEmail;

public record ManuallyConfirmEmailCommand(string UserId) : IRequest<ManuallyConfirmEmailResult>;