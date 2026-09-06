using Domain.DTOs.Account;
using Domain.DTOs.Account.Responses;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Account.Commands.UpdateProfile;

public class UpdateProfileCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<UpdateProfileCommand, UpdateProfileResult>
{
    public async Task<UpdateProfileResult> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            return UpdateProfileResult.Failure("Bruker ikke funnet.");

        // Beskyttelse mot endring av systemadministrator
        if (await userManager.IsInRoleAsync(user, "Admin"))
            return UpdateProfileResult.Forbidden(
                "Profilinformasjonen til systemadministrator er låst og kan ikke endres.");

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.LastModifiedAt = DateTime.UtcNow;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return UpdateProfileResult.Failure(result.Errors);

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

        return UpdateProfileResult.Success(profile);
    }
}