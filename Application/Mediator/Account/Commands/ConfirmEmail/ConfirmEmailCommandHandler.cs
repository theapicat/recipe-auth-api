using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Account.Commands.ConfirmEmail;

public class ConfirmEmailCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<ConfirmEmailCommand, ConfirmEmailResult>
{
    public async Task<ConfirmEmailResult> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return ConfirmEmailResult.NotFound();

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
            return ConfirmEmailResult.Failure(result.Errors);

        // Fjerner sperren dersom brukeren var låst pga. ubekreftet e-post
        if (await userManager.IsLockedOutAsync(user))
        {
            await userManager.SetLockoutEndDateAsync(user, null);
            user.LockoutReason = LockoutReason.None;
            user.LockoutReasonDetails = null;
        }

        user.LastModifiedAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        return ConfirmEmailResult.Success();
    }
}