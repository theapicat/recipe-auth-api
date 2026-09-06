using Contracts.Events.AdminActions;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Admin.Commands.SendUserEmailAdmin;

public class SendUserEmailAdminCommandHandler(
    UserManager<ApplicationUser> userManager,
    IPublishEndpoint publishEndpoint)
    : IRequestHandler<SendUserEmailAdminCommand, SendUserEmailAdminResult>
{
    public async Task<SendUserEmailAdminResult> Handle(
        SendUserEmailAdminCommand request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.UserId, out var targetGuid))
            return SendUserEmailAdminResult.BadRequest("Ugyldig bruker-ID oppgitt.");

        if (string.IsNullOrWhiteSpace(request.Subject))
            return SendUserEmailAdminResult.BadRequest("Emne kan ikke være tomt.");

        if (string.IsNullOrWhiteSpace(request.Message))
            return SendUserEmailAdminResult.BadRequest("Melding kan ikke være tom.");

        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return SendUserEmailAdminResult.NotFound();

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;

        await publishEndpoint.Publish(new AdminCustomEmailRequestedEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            Name = displayName,
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            SentAt = DateTime.UtcNow
        }, cancellationToken);

        return SendUserEmailAdminResult.Success(user.Email!);
    }
}