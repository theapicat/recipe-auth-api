public record ProcessGoogleCallbackResult(
    bool IsSuccess, 
    string? RedirectUrl = null, 
    string? ErrorMessage = null);