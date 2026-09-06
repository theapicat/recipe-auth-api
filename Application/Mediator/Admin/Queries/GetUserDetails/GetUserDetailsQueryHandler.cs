using Domain.DTOs.Admin;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Persistence.Context;

namespace Application.Mediator.Admin.Queries.GetUserDetails;

public class GetUserDetailsQueryHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<GetUserDetailsQuery, GetUserDetailsResult>
{
    public async Task<GetUserDetailsResult> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null) return GetUserDetailsResult.NotFound();

        var roles = await userManager.GetRolesAsync(user);
        var isLocked = await userManager.IsLockedOutAsync(user);
        var hasPassword = await userManager.HasPasswordAsync(user);
        var logins = await userManager.GetLoginsAsync(user);

        var details = new AdminUserDetailsDto
        {
            UserId = user.Id.ToString(),
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = roles.FirstOrDefault() ?? "user",
            HasPassword = hasPassword,
            IsGoogleAccount = logins.Any(l => l.LoginProvider == "Google"),
            IsEmailConfirmed = user.EmailConfirmed,
            WelcomeCompleted = user.WelcomeCompleted,
            IsLocked = isLocked,
            LockoutEnd = user.LockoutEnd,
            LockoutReason = user.LockoutReason.ToString(),
            LockoutReasonDetails = user.LockoutReasonDetails,
            AccessFailedCount = user.AccessFailedCount,
            CreatedAt = user.CreatedAt,
            LastModifiedAt = user.LastModifiedAt,
            LastLoginAt = user.LastLoginAt,

            Confirmation7DaysReminderSentAt = user.Confirmation7DaysReminderSentAt,
            Confirmation14DaysLockedSentAt = user.Confirmation14DaysLockedSentAt,
            InactivityWarning6MonthsSentAt = user.InactivityWarning6MonthsSentAt,
            Inactivity1YearLockedSentAt = user.Inactivity1YearLockedSentAt
        };

        return GetUserDetailsResult.Success(details);
    }
}