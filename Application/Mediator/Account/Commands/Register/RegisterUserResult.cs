using Application.Mediator.Common;
using Domain.DTOs.Account.Responses;
using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.Register;

public class RegisterUserResult : OperationResult
{
    public UserProfileResponse? UserProfile { get; set; }

    public static RegisterUserResult Success(UserProfileResponse profile)
    {
        return new RegisterUserResult { IsSuccess = true, UserProfile = profile };
    }

    public static RegisterUserResult Failure(string message)
    {
        return new RegisterUserResult { IsSuccess = false, ErrorMessage = message };
    }

    public static RegisterUserResult Failure(IEnumerable<IdentityError> errors)
    {
        return new RegisterUserResult { IsSuccess = false, Errors = errors };
    }
}
