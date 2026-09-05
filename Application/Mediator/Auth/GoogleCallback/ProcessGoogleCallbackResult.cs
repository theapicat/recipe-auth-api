namespace Application.Mediator.Auth.GoogleCallback;

public record ProcessGoogleCallbackResult(
    bool IsSuccess, 
    string? RedirectUrl = null, 
    string? ErrorMessage = null);