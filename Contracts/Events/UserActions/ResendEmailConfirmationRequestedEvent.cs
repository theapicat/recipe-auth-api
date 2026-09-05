namespace Contracts.Events;

public record ResendEmailConfirmationRequestedEvent
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
    public required string ConfirmationLink { get; init; }
    public required DateTime RequestedAt { get; init; }
}