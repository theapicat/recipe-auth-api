using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.SetPassword;

public class SetPasswordResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }

    public static SetPasswordResult Success()
    {
        return new SetPasswordResult { IsSuccess = true };
    }

    public static SetPasswordResult Failure(string message)
    {
        return new SetPasswordResult { IsSuccess = false, ErrorMessage = message };
    }

    public static SetPasswordResult Failure(IEnumerable<IdentityError> errors)
    {
        return new SetPasswordResult { IsSuccess = false, Errors = errors };
    }
}