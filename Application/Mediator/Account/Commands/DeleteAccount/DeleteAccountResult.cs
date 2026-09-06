using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.DeleteAccount;

public class DeleteAccountResult
{
    public bool IsSuccess { get; set; }
    public bool IsForbidden { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }

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