namespace Contracts.Events.UserActions;

public record PasswordChangedEvent
{
    public required Guid UserId { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required DateTime ChangedAt { get; init; }
    public string? IpAddress { get; init; }
    public string? DeviceInfo { get; init; } // F.eks. "Chrome på Linux"
}