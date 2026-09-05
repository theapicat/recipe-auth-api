namespace Contracts.Events.UserActions;

public record UserRegisteredWithGoogleEvent
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; } = string.Empty;
    public required string Name { get; init; } = string.Empty;
    public required DateTime RegisteredAt { get; init; }
}