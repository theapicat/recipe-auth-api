using Application.Mediator.Common;
using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.DeleteAccount;

public class DeleteAccountResult : OperationResult
{
    public bool IsForbidden { get; set; }

    public static DeleteAccountResult Success()
    {
        return new DeleteAccountResult { IsSuccess = true };
    }

    public static DeleteAccountResult Forbidden(string message)
    {
        return new DeleteAccountResult { IsSuccess = false, IsForbidden = true, ErrorMessage = message };
    }

    public static DeleteAccountResult Failure(string message)
    {
        return new DeleteAccountResult { IsSuccess = false, ErrorMessage = message };
    }

    public static DeleteAccountResult Failure(IEnumerable<IdentityError> errors)
    {
        return new DeleteAccountResult { IsSuccess = false, Errors = errors };
    }
}
