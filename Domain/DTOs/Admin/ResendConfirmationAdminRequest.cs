namespace Domain.DTOs.Admin;

public record ResendConfirmationAdminRequest
{
    public required string UserId { get; init; }
}