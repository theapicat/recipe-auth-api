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

    public static DeleteUserAdminResult Success(string email) => new() { IsSuccess = true, TargetEmail = email };
    public static DeleteUserAdminResult BadRequest(string message) => new() { IsSuccess = false, IsBadRequest = true, ErrorMessage = message };
    public static DeleteUserAdminResult NotFound(string message = "Bruker ikke funnet.") => new() { IsSuccess = false, IsNotFound = true, ErrorMessage = message };
    public static DeleteUserAdminResult Failure(IEnumerable<IdentityError> errors) => new() { IsSuccess = false, Errors = errors };
}