using Domain.DTOs.Account;
using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.CompleteWelcome;

public class CompleteWelcomeResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }
    public UserProfileResponse? UserProfile { get; set; }

    public static CompleteWelcomeResult Success(UserProfileResponse profile) => new() { IsSuccess = true, UserProfile = profile };
    public static CompleteWelcomeResult Failure(string message) => new() { IsSuccess = false, ErrorMessage = message };
    public static CompleteWelcomeResult Failure(IEnumerable<IdentityError> errors) => new() { IsSuccess = false, Errors = errors };
}