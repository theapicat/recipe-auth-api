using Domain.DTOs.Account;
using Domain.DTOs.Account.Responses;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Account.Commands.CompleteWelcome;

public class CompleteWelcomeCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<CompleteWelcomeCommand, CompleteWelcomeResult>
{
    public async Task<CompleteWelcomeResult> Handle(CompleteWelcomeCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            return CompleteWelcomeResult.Failure("Bruker ikke funnet.");

        user.WelcomeCompleted = true;
        user.LastModifiedAt = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return CompleteWelcomeResult.Failure(result.Errors);

        var roles = await userManager.GetRolesAsync(user);
        var isLocked = await userManager.IsLockedOutAsync(user);
        var hasPassword = await userManager.HasPasswordAsync(user);
        var logins = await userManager.GetLoginsAsync(user);
        var isGoogleAccount = logins.Any(l => l.LoginProvider == "Google");

        var profile = new UserProfileResponse
        {
            UserId = user.Id.ToString(),
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = roles.FirstOrDefault() ?? "user",
            HasPassword = hasPassword,
            IsGoogleAccount = isGoogleAccount,
            IsEmailConfirmed = user.EmailConfirmed,
            WelcomeCompleted = user.WelcomeCompleted,
            IsLocked = isLocked,
            CreatedAt = user.CreatedAt,
            LastModifiedAt = user.LastModifiedAt,
            LastLoginAt = user.LastLoginAt
        };

        return CompleteWelcomeResult.Success(profile);
    }
}