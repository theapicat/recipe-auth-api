namespace Contracts.Events.SystemActions;

public record UserAccountDeletedBySystemEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DeletionReason { get; init; } = string.Empty;
    public DateTime DeletedAt { get; init; }
}