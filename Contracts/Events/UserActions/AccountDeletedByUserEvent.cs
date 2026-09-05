namespace Contracts.Events;

public record UserAccountDeletedByUserEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTime DeletedAt { get; init; }
}