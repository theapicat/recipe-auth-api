using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Admin.Commands.DeleteUserAdmin;

public class DeleteUserAdminResult
{
    public bool IsSuccess { get; set; }
    public bool IsBadRequest { get; set; }
    public bool IsNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }
    public string? TargetEmail { get; set; }

    public static DeleteUserAdminResult Success(string email)
    {
        return new DeleteUserAdminResult { IsSuccess = true, TargetEmail = email };
    }

    public static DeleteUserAdminResult BadRequest(string message)
    {
        return new DeleteUserAdminResult { IsSuccess = false, IsBadRequest = true, ErrorMessage = message };
    }

    public static DeleteUserAdminResult NotFound(string message = "Bruker ikke funnet.")
    {
        return new DeleteUserAdminResult { IsSuccess = false, IsNotFound = true, ErrorMessage = message };
    }

    public static DeleteUserAdminResult Failure(IEnumerable<IdentityError> errors)
    {
        return new DeleteUserAdminResult { IsSuccess = false, Errors = errors };
    }
}