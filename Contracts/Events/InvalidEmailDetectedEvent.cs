namespace Contracts.Events;

public class InvalidEmailDetectedEvent
{
    public Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string Reason { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}