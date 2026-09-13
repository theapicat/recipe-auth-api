using Application.Mediator.Common;
using Domain.DTOs.Account.Responses;
using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.CompleteWelcome;

public class CompleteWelcomeResult : OperationResult
{
    public UserProfileResponse? UserProfile { get; set; }

    public static CompleteWelcomeResult Success(UserProfileResponse profile)
    {
        return new CompleteWelcomeResult { IsSuccess = true, UserProfile = profile };
    }

    public static CompleteWelcomeResult Failure(string message)
    {
        return new CompleteWelcomeResult { IsSuccess = false, ErrorMessage = message };
    }

    public static CompleteWelcomeResult Failure(IEnumerable<IdentityError> errors)
    {
        return new CompleteWelcomeResult { IsSuccess = false, Errors = errors };
    }
}
