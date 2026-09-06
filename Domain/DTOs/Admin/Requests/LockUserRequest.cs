namespace Domain.DTOs.Admin.Requests;

public record LockUserRequest
{
    public required string UserId { get; init; }
    public string? ReasonDetails { get; init; }
}