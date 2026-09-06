namespace Application.Mediator.Account.Commands.GoogleCallback;

public record ProcessGoogleCallbackResult(
    bool IsSuccess,
    string? RedirectUrl = null,
    string? ErrorMessage = null);