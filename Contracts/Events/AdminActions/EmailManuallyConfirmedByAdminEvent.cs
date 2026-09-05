namespace Contracts.Events.AdminActions;

public record EmailManuallyConfirmedByAdminEvent
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required DateTime ConfirmedAt { get; init; }
}