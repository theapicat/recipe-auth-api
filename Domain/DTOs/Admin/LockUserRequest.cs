namespace Domain.DTOs.Admin;

public record LockUserRequest
{
    public required string UserId { get; init; }
    public string? ReasonDetails { get; init; }
}