using Application.Mediator.Common;

namespace Application.Mediator.Admin.Commands.LockUser;

public class LockUserResult : OperationResult
{
    public bool IsBadRequest { get; set; }
    public bool IsNotFound { get; set; }
    public string? TargetEmail { get; set; }

    public static LockUserResult Success(string email)
    {
        return new LockUserResult { IsSuccess = true, TargetEmail = email };
    }

    public static LockUserResult BadRequest(string message)
    {
        return new LockUserResult { IsSuccess = false, IsBadRequest = true, ErrorMessage = message };
    }

    public static LockUserResult NotFound(string message = "Bruker ikke funnet.")
    {
        return new LockUserResult { IsSuccess = false, IsNotFound = true, ErrorMessage = message };
    }
}
