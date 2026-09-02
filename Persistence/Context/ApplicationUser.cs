using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Persistence.Context;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public bool WelcomeCompleted { get; set; } = false;
    
    // Tidsstempler for aktivitet
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // ----------------------------------------------------
    // Sporede tidsstempler for kontolivssyklus (Idempotens)
    // ----------------------------------------------------
    public DateTime? Confirmation7DaysReminderSentAt { get; set; }
    public DateTime? Confirmation14DaysLockedSentAt { get; set; }
    public DateTime? InactivityWarning6MonthsSentAt { get; set; }
    public DateTime? Inactivity1YearLockedSentAt { get; set; }

    // ----------------------------------------------------
    // Sperre- og sletteårsak (Enum + Valgfri detaljtekst)
    // ----------------------------------------------------
    public LockoutReason LockoutReason { get; set; } = LockoutReason.None;
    public string? LockoutReasonDetails { get; set; }
}