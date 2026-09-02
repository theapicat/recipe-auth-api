namespace Domain.Enums;

public enum DeleteReason
{
    None = 0,
    UnconfirmedEmail30Days = 1,
    Inactivity1YearPlus30Days = 2,
    UserRequested = 3,
    ManualAdminDelete = 4
}