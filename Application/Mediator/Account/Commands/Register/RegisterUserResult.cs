using Domain.DTOs.Account;
using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.Register;

public class RegisterUserResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }
    public UserProfileResponse? UserProfile { get; set; }

    public static RegisterUserResult Success(UserProfileResponse profile) =>
        new() { IsSuccess = true, UserProfile = profile };

    public static RegisterUserResult Failure(string message) =>
        new() { IsSuccess = false, ErrorMessage = message };

    public static RegisterUserResult Failure(IEnumerable<IdentityError> errors) =>
        new() { IsSuccess = false, Errors = errors };
}