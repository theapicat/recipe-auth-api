namespace Domain.DTOs.Account.Requests;

public class RecoverPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}