namespace Application.Mediator.Admin.Commands.ManuallyConfirmEmail;

public class ManuallyConfirmEmailResult
{
    public bool IsSuccess { get; set; }
    public bool IsNotFound { get; set; }
    public bool IsAlreadyConfirmed { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TargetEmail { get; set; }

    public static ManuallyConfirmEmailResult Success(string email) => new() { IsSuccess = true, TargetEmail = email };
    public static ManuallyConfirmEmailResult NotFound(string message = "Bruker ikke funnet.") => new() { IsSuccess = false, IsNotFound = true, ErrorMessage = message };
    public static ManuallyConfirmEmailResult AlreadyConfirmed(string message = "E-posten til denne brukeren er allerede bekreftet.") => new() { IsSuccess = false, IsAlreadyConfirmed = true, ErrorMessage = message };
}