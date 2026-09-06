namespace Application.Mediator.Admin.Commands.AddBlacklistEntry;

public class AddBlacklistEntryResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    public static AddBlacklistEntryResult Success()
    {
        return new AddBlacklistEntryResult { IsSuccess = true };
    }

    public static AddBlacklistEntryResult Failure(string message)
    {
        return new AddBlacklistEntryResult { IsSuccess = false, ErrorMessage = message };
    }
}