using Application.Mediator.Common;
using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.ResetPassword;

public class ResetPasswordResult : OperationResult
{
    public bool IsNotFound { get; set; }

    public static ResetPasswordResult Success()
    {
        return new ResetPasswordResult { IsSuccess = true };
    }

    public static ResetPasswordResult NotFound()
    {
        return new ResetPasswordResult { IsSuccess = false, IsNotFound = true, ErrorMessage = "Bruker ikke funnet." };
    }

    public static ResetPasswordResult Failure(IEnumerable<IdentityError> errors)
    {
        return new ResetPasswordResult
            { IsSuccess = false, ErrorMessage = "Tilbakestilling mislyktes.", Errors = errors };
    }
}
