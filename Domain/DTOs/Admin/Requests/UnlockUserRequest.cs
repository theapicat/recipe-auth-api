namespace Domain.DTOs.Admin.Requests;

public record UnlockUserRequest
{
    public required string UserId { get; init; }
}