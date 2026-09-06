using Domain.DTOs.Account;
using Domain.DTOs.Account.Responses;

namespace Application.Mediator.Account.Queries.GetUserProfile;

public class GetUserProfileResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public UserProfileResponse? UserProfile { get; set; }

    public static GetUserProfileResult Success(UserProfileResponse profile)
    {
        return new GetUserProfileResult { IsSuccess = true, UserProfile = profile };
    }

    public static GetUserProfileResult Failure(string message)
    {
        return new GetUserProfileResult { IsSuccess = false, ErrorMessage = message };
    }
}