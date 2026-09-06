using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.SetPassword;

public class SetPasswordResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }

    public static SetPasswordResult Success() => new() { IsSuccess = true };
    public static SetPasswordResult Failure(string message) => new() { IsSuccess = false, ErrorMessage = message };
    public static SetPasswordResult Failure(IEnumerable<IdentityError> errors) => new() { IsSuccess = false, Errors = errors };
}