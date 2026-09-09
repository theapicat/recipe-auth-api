namespace Contracts.Events.SystemActions;

public class Inactivity6MonthsWarningEvent
{
    public Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public DateTime WarnedAt { get; set; } = DateTime.UtcNow;
}