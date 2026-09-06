using System.Security.Claims;

namespace Application.Mediator.Authorization.Commands.PasswordGrant;

public class TokenExchangeResult
{
    public bool IsSuccess { get; set; }
    public ClaimsPrincipal? Principal { get; set; }
    public string? ErrorDescription { get; set; }

    public static TokenExchangeResult Success(ClaimsPrincipal principal) =>
        new() { IsSuccess = true, Principal = principal };

    public static TokenExchangeResult Failure(string errorDescription) =>
        new() { IsSuccess = false, ErrorDescription = errorDescription };
}