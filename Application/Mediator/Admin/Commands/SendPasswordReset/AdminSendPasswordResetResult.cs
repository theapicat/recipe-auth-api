namespace Application.Mediator.Admin.Commands.SendPasswordReset;

public class AdminSendPasswordResetResult
{
    public bool IsSuccess { get; set; }
    public bool IsNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TargetEmail { get; set; }

    public static AdminSendPasswordResetResult Success(string email) => new() { IsSuccess = true, TargetEmail = email };
    public static AdminSendPasswordResetResult NotFound(string message = "Bruker ikke funnet.") => new() { IsSuccess = false, IsNotFound = true, ErrorMessage = message };
}