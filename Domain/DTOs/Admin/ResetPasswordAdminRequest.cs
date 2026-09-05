namespace Domain.DTOs.Admin;

public record ResetPasswordAdminRequest
{
    public required string UserId { get; init; }
}