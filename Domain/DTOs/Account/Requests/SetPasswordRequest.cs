namespace Domain.DTOs.Account.Requests;

public class SetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}