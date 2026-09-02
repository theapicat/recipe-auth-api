namespace Domain.Enums;

public enum LockoutReason
{
    None = 0,
    UnconfirmedEmail14Days = 1,
    Inactivity1Year = 2,
    ManualAdminLock = 3
}