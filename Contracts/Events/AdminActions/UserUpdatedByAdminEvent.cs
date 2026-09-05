namespace Contracts.Events.AdminActions;

public record UserUpdatedByAdminEvent
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string OldEmail { get; init; }
    public required string NewEmail { get; init; }
    public required DateTime UpdatedAt { get; init; }
}