namespace Application.Mediator.Admin.Commands.SendUserEmailAdmin;

public class SendUserEmailAdminResult
{
    public bool IsSuccess { get; set; }
    public bool IsBadRequest { get; set; }
    public bool IsNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TargetEmail { get; set; }

    public static SendUserEmailAdminResult Success(string email) => 
        new() { IsSuccess = true, TargetEmail = email };

    public static SendUserEmailAdminResult BadRequest(string message) => 
        new() { IsSuccess = false, IsBadRequest = true, ErrorMessage = message };

    public static SendUserEmailAdminResult NotFound(string message = "Bruker ikke funnet.") => 
        new() { IsSuccess = false, IsNotFound = true, ErrorMessage = message };
}