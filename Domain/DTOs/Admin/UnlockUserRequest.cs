namespace Domain.DTOs.Admin;

public record UnlockUserRequest
{
    public required string UserId { get; init; }
}