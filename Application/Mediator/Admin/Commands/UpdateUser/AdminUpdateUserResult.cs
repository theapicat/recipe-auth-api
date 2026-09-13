using Application.Mediator.Common;
using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Admin.Commands.UpdateUser;

public class AdminUpdateUserResult : OperationResult
{
    public bool IsBadRequest { get; set; }
    public bool IsNotFound { get; set; }

    public static AdminUpdateUserResult Success()
    {
        return new AdminUpdateUserResult { IsSuccess = true };
    }

    public static AdminUpdateUserResult BadRequest(string message)
    {
        return new AdminUpdateUserResult { IsSuccess = false, IsBadRequest = true, ErrorMessage = message };
    }

    public static AdminUpdateUserResult NotFound(string message = "Bruker ikke funnet.")
    {
        return new AdminUpdateUserResult { IsSuccess = false, IsNotFound = true, ErrorMessage = message };
    }

    public static AdminUpdateUserResult Failure(IEnumerable<IdentityError> errors)
    {
        return new AdminUpdateUserResult { IsSuccess = false, Errors = errors };
    }
}
