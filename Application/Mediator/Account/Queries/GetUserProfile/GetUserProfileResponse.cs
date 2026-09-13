using Application.Mediator.Common;
using Domain.DTOs.Account.Responses;

namespace Application.Mediator.Account.Queries.GetUserProfile;

public class GetUserProfileResult : OperationResult
{
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
