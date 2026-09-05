namespace Contracts.Events;

public record Confirmation7DaysReminderEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string ConfirmationLink { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
}