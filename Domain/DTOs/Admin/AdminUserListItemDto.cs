namespace Domain.DTOs.Admin;

public record AdminUserListItemDto
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Role { get; init; }
    public required bool IsEmailConfirmed { get; init; }
    public required bool IsLocked { get; init; }
    public required bool IsGoogleAccount { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}