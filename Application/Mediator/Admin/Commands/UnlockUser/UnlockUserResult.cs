namespace Application.Mediator.Admin.Commands.UnlockUser;

public class UnlockUserResult
{
    public bool IsSuccess { get; set; }
    public bool IsNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TargetEmail { get; set; }

    public static UnlockUserResult Success(string email)
    {
        return new UnlockUserResult { IsSuccess = true, TargetEmail = email };
    }

    public static UnlockUserResult NotFound(string message = "Bruker ikke funnet.")
    {
        return new UnlockUserResult { IsSuccess = false, IsNotFound = true, ErrorMessage = message };
    }
}