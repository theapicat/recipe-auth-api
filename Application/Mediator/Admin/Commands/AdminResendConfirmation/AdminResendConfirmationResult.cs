namespace Application.Mediator.Admin.Commands.AdminResendConfirmation;

public class AdminResendConfirmationResult
{
    public bool IsSuccess { get; set; }
    public bool IsNotFound { get; set; }
    public bool IsAlreadyConfirmed { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TargetEmail { get; set; }

    public static AdminResendConfirmationResult Success(string email) => new() { IsSuccess = true, TargetEmail = email };
    public static AdminResendConfirmationResult NotFound(string message = "Bruker ikke funnet.") => new() { IsSuccess = false, IsNotFound = true, ErrorMessage = message };
    public static AdminResendConfirmationResult AlreadyConfirmed(string message = "E-posten er allerede bekreftet.") => new() { IsSuccess = false, IsAlreadyConfirmed = true, ErrorMessage = message };
}