using Domain.DTOs.Account;
using Domain.DTOs.Account.Responses;
using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.UpdateProfile;

public class UpdateProfileResult
{
    public bool IsSuccess { get; set; }
    public bool IsForbidden { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }
    public UserProfileResponse? UserProfile { get; set; }

    public static UpdateProfileResult Success(UserProfileResponse profile)
    {
        return new UpdateProfileResult { IsSuccess = true, UserProfile = profile };
    }

    public static UpdateProfileResult Forbidden(string message)
    {
        return new UpdateProfileResult { IsSuccess = false, IsForbidden = true, ErrorMessage = message };
    }

    public static UpdateProfileResult Failure(string message)
    {
        return new UpdateProfileResult { IsSuccess = false, ErrorMessage = message };
    }

    public static UpdateProfileResult Failure(IEnumerable<IdentityError> errors)
    {
        return new UpdateProfileResult { IsSuccess = false, Errors = errors };
    }
}