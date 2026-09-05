namespace Contracts.Events.UserActions;

public record ContactFormSubmittedEvent
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Subject { get; init; }
    public required string Message { get; init; }
    public DateTime SubmittedAt { get; init; }
}