using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.DeleteAccount;

public class DeleteAccountResult
{
    public bool IsSuccess { get; set; }
    public bool IsForbidden { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }

    public static DeleteAccountResult Success() => new() { IsSuccess = true };
    public static DeleteAccountResult Forbidden(string message) => new() { IsSuccess = false, IsForbidden = true, ErrorMessage = message };
    public static DeleteAccountResult Failure(string message) => new() { IsSuccess = false, ErrorMessage = message };
    public static DeleteAccountResult Failure(IEnumerable<IdentityError> errors) => new() { IsSuccess = false, Errors = errors };
}