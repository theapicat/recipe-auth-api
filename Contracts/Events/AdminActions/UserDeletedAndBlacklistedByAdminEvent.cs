namespace Contracts.Events.AdminActions;

public record UserDeletedAndBlacklistedByAdminEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public DateTime DeletedAt { get; init; }
}