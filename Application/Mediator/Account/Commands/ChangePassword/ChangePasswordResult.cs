using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.ChangePassword;

public class ChangePasswordResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }

    public static ChangePasswordResult Success() => new() { IsSuccess = true };
    public static ChangePasswordResult Failure(string message) => new() { IsSuccess = false, ErrorMessage = message };
    public static ChangePasswordResult Failure(IEnumerable<IdentityError> errors) => new() { IsSuccess = false, Errors = errors };
}