namespace Domain.DTOs.Admin;

public record AdminUserDetailsDto
{
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Role { get; init; }
    public required bool HasPassword { get; init; }
    public required bool IsGoogleAccount { get; init; }
    public required bool IsEmailConfirmed { get; init; }
    public required bool WelcomeCompleted { get; init; }
    public required bool IsLocked { get; init; }
    public DateTimeOffset? LockoutEnd { get; init; }
    public required string LockoutReason { get; init; }
    public string? LockoutReasonDetails { get; init; }
    public required int AccessFailedCount { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime LastModifiedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }

    // Tidsstempler for kontolivssyklus
    public DateTime? Confirmation7DaysReminderSentAt { get; init; }
    public DateTime? Confirmation14DaysLockedSentAt { get; init; }
    public DateTime? InactivityWarning6MonthsSentAt { get; init; }
    public DateTime? Inactivity1YearLockedSentAt { get; init; }
}