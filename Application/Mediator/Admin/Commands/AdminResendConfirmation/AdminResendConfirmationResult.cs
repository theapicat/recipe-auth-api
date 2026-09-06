namespace Application.Mediator.Admin.Commands.AdminResendConfirmation;

public class AdminResendConfirmationResult
{
    public bool IsSuccess { get; set; }
    public bool IsNotFound { get; set; }
    public bool IsAlreadyConfirmed { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TargetEmail { get; set; }

    public static AdminResendConfirmationResult Success(string email)
    {
        return new AdminResendConfirmationResult { IsSuccess = true, TargetEmail = email };
    }

    public static AdminResendConfirmationResult NotFound(string message = "Bruker ikke funnet.")
    {
        return new AdminResendConfirmationResult { IsSuccess = false, IsNotFound = true, ErrorMessage = message };
    }

    public static AdminResendConfirmationResult AlreadyConfirmed(string message = "E-posten er allerede bekreftet.")
    {
        return new AdminResendConfirmationResult
            { IsSuccess = false, IsAlreadyConfirmed = true, ErrorMessage = message };
    }
}