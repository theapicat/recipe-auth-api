namespace Contracts.Events;

public record UserAccountDeletedEvent
{
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required DateTime DeletedAt { get; init; }
}