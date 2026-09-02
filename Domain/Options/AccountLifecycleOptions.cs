namespace Domain.Options;

public class AccountLifecycleOptions
{
    public const string SectionName = "AccountLifecycle";

    public string CronSchedule { get; set; } = "0 0 3 * * ?"; // Standard: Hver natt kl 03:00

    public int ConfirmationReminderDays { get; set; } = 7;
    public int ConfirmationLockoutDays { get; set; } = 14;
    public int ConfirmationDeletionDays { get; set; } = 30;

    public int InactivityWarningMonths { get; set; } = 6;
    public int InactivityLockoutYears { get; set; } = 1;
    public int InactivityDeletionDays { get; set; } = 30;
}