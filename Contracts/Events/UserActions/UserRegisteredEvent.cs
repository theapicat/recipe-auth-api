namespace Contracts.Events.UserActions;

public record UserRegisteredEvent
{
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string ConfirmationLink { get; init; }
    public DateTime RegisteredAt { get; init; }
}