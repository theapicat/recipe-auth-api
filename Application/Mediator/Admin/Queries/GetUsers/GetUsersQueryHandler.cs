using Domain.DTOs.Admin;
using Domain.DTOs.Admin.Responses;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Application.Mediator.Admin.Queries.GetUsers;

public class GetUsersQueryHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<GetUsersQuery, List<AdminUserListItemResponse>>
{
    public async Task<List<AdminUserListItemResponse>> Handle(GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);

        var userListItems = new List<AdminUserListItemResponse>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var isLocked = await userManager.IsLockedOutAsync(user);
            var logins = await userManager.GetLoginsAsync(user);

            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            var displayName = string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? string.Empty : fullName;

            userListItems.Add(new AdminUserListItemResponse
            {
                UserId = user.Id.ToString(),
                Email = user.Email ?? string.Empty,
                FullName = displayName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.FirstOrDefault() ?? "user",
                IsEmailConfirmed = user.EmailConfirmed,
                IsLocked = isLocked,
                IsGoogleAccount = logins.Any(l => l.LoginProvider == "Google"),
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }

        return userListItems;
    }
}