using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Admin.Commands.DeleteAndBlacklistUser;

public class DeleteAndBlacklistUserResult
{
    public bool IsSuccess { get; set; }
    public bool IsBadRequest { get; set; }
    public bool IsNotFound { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<IdentityError>? Errors { get; set; }
    public string? TargetEmail { get; set; }

    public static DeleteAndBlacklistUserResult Success(string email) => new() { IsSuccess = true, TargetEmail = email };
    public static DeleteAndBlacklistUserResult BadRequest(string message) => new() { IsSuccess = false, IsBadRequest = true, ErrorMessage = message };
    public static DeleteAndBlacklistUserResult NotFound(string message = "Bruker ikke funnet.") => new() { IsSuccess = false, IsNotFound = true, ErrorMessage = message };
    public static DeleteAndBlacklistUserResult Failure(IEnumerable<IdentityError> errors) => new() { IsSuccess = false, Errors = errors };
}