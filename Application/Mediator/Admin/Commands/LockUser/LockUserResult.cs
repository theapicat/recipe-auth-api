namespace Application.Mediator.Admin.Commands.UserLockCommand;

public class LockUserResult
{
    public bool IsSuccess { get; set; }
    public bool IsBadRequest { get; set; }
    public bool IsNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TargetEmail { get; set; }

    public static LockUserResult Success(string email) => new() { IsSuccess = true, TargetEmail = email };
    public static LockUserResult BadRequest(string message) => new() { IsSuccess = false, IsBadRequest = true, ErrorMessage = message };
    public static LockUserResult NotFound(string message = "Bruker ikke funnet.") => new() { IsSuccess = false, IsNotFound = true, ErrorMessage = message };
}