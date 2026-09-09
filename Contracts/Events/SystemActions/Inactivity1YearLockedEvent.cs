namespace Contracts.Events.SystemActions;

public class Inactivity1YearLockedEvent
{
    public Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public DateTime LockedAt { get; set; } = DateTime.UtcNow;
}