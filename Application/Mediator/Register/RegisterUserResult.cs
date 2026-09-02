using Microsoft.AspNetCore.Identity;

namespace Application.Mediator.Register;

public record RegisterUserResult(
    bool IsSuccess,
    string? ErrorMessage = null,
    IEnumerable<IdentityError>? Errors = null,
    UserProfileResponse? UserProfile = null
);