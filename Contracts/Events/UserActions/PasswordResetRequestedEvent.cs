namespace Contracts.Events.UserActions;

public record PasswordResetRequestedEvent
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; } = string.Empty;
    public required string Name { get; init; } = string.Empty;
    public required string ResetLink { get; init; } = string.Empty;
    public required DateTime RequestedAt { get; init; }
}