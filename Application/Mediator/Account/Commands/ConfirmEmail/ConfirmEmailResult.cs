using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Account.Commands.ConfirmEmail;

public class ConfirmEmailResult
{
    public bool IsSuccess { get; set; }
    public bool IsNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }

    public static ConfirmEmailResult Success()
    {
        return new ConfirmEmailResult { IsSuccess = true };
    }

    public static ConfirmEmailResult NotFound()
    {
        return new ConfirmEmailResult { IsSuccess = false, IsNotFound = true, ErrorMessage = "Bruker ikke funnet." };
    }

    public static ConfirmEmailResult Failure(IEnumerable<IdentityError> errors)
    {
        return new ConfirmEmailResult
            { IsSuccess = false, ErrorMessage = "Ugyldig eller utløpt bekreftelseskode.", Errors = errors };
    }
}