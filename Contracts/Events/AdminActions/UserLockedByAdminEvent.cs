namespace Contracts.Events.AdminActions;

public record UserLockedByAdminEvent
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string ReasonDetails { get; init; }
    public required DateTime LockedAt { get; init; }
}