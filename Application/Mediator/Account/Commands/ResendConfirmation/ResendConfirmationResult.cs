using Application.Mediator.Common;

namespace Application.Mediator.Account.Commands.ResendConfirmation;

public class ResendConfirmationResult : OperationResult
{
    public bool IsAlreadyConfirmed { get; set; }

    public static ResendConfirmationResult Success()
    {
        return new ResendConfirmationResult { IsSuccess = true };
    }

    public static ResendConfirmationResult AlreadyConfirmed()
    {
        return new ResendConfirmationResult
        {
            IsSuccess = false, IsAlreadyConfirmed = true, ErrorMessage = "E-postadressen din er allerede bekreftet."
        };
    }

    public static ResendConfirmationResult Failure(string message)
    {
        return new ResendConfirmationResult { IsSuccess = false, ErrorMessage = message };
    }
}
