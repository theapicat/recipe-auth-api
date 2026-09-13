using Application.Mediator.Common;

namespace Application.Mediator.Admin.Commands.SendUserEmailAdmin;

public class SendUserEmailAdminResult : OperationResult
{
    public bool IsBadRequest { get; set; }
    public bool IsNotFound { get; set; }
    public string? TargetEmail { get; set; }

    public static SendUserEmailAdminResult Success(string email)
    {
        return new SendUserEmailAdminResult { IsSuccess = true, TargetEmail = email };
    }

    public static SendUserEmailAdminResult BadRequest(string message)
    {
        return new SendUserEmailAdminResult { IsSuccess = false, IsBadRequest = true, ErrorMessage = message };
    }

    public static SendUserEmailAdminResult NotFound(string message = "Bruker ikke funnet.")
    {
        return new SendUserEmailAdminResult { IsSuccess = false, IsNotFound = true, ErrorMessage = message };
    }
}
