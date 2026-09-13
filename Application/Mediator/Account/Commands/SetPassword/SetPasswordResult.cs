using Application.Mediator.Common;
using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.SetPassword;

public class SetPasswordResult : OperationResult
{
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
