namespace Domain.DTOs.Admin.Requests;

public record ResendConfirmationAdminRequest
{
    public required string UserId { get; init; }
}