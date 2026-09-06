namespace Application.Mediator.Account.Commands.ResendConfirmation;

public class ResendConfirmationResult
{
    public bool IsSuccess { get; set; }
    public bool IsAlreadyConfirmed { get; set; }
    public string? ErrorMessage { get; set; }

    public static ResendConfirmationResult Success() => new() { IsSuccess = true };
    public static ResendConfirmationResult AlreadyConfirmed() => new() { IsSuccess = false, IsAlreadyConfirmed = true, ErrorMessage = "E-postadressen din er allerede bekreftet." };
    public static ResendConfirmationResult Failure(string message) => new() { IsSuccess = false, ErrorMessage = message };
}