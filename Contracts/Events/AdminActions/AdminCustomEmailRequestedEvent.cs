namespace Contracts.Events.AdminActions;

public record AdminCustomEmailRequestedEvent
{
    public Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string Subject { get; init; }
    public required string Message { get; init; }
    public DateTime SentAt { get; init; }
}