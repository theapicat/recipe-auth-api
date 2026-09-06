using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.ConfirmEmail;

public class ConfirmEmailResult
{
    public bool IsSuccess { get; set; }
    public bool IsNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }

    public static ConfirmEmailResult Success() => new() { IsSuccess = true };
    public static ConfirmEmailResult NotFound() => new() { IsSuccess = false, IsNotFound = true, ErrorMessage = "Bruker ikke funnet." };
    public static ConfirmEmailResult Failure(IEnumerable<IdentityError> errors) => 
        new() { IsSuccess = false, ErrorMessage = "Ugyldig eller utløpt bekreftelseskode.", Errors = errors };
}