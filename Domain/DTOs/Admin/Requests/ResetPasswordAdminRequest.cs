namespace Domain.DTOs.Admin.Requests;

public record ResetPasswordAdminRequest
{
    public required string UserId { get; init; }
}