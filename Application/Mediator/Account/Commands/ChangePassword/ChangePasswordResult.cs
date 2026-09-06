using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.ChangePassword;

public class ChangePasswordResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }

    public static ChangePasswordResult Success()
    {
        return new ChangePasswordResult { IsSuccess = true };
    }

    public static ChangePasswordResult Failure(string message)
    {
        return new ChangePasswordResult { IsSuccess = false, ErrorMessage = message };
    }

    public static ChangePasswordResult Failure(IEnumerable<IdentityError> errors)
    {
        return new ChangePasswordResult { IsSuccess = false, Errors = errors };
    }
}