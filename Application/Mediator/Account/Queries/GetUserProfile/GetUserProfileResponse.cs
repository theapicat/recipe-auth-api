using Domain.DTOs.Account;

namespace Application.Mediator.Account.Queries.GetUserProfile;

public class GetUserProfileResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public UserProfileResponse? UserProfile { get; set; }

    public static GetUserProfileResult Success(UserProfileResponse profile) =>
        new() { IsSuccess = true, UserProfile = profile };

    public static GetUserProfileResult Failure(string message) =>
        new() { IsSuccess = false, ErrorMessage = message };
}