namespace Application.Mediator.Account.Commands.RecoverPassword;

public class RecoverPasswordResult
{
    public bool IsSuccess { get; set; }

    public string Message { get; set; } =
        "Dersom e-posten er registrert, har instruksjoner om tilbakestilling blitt sendt.";

    public static RecoverPasswordResult Success()
    {
        return new RecoverPasswordResult { IsSuccess = true };
    }
}