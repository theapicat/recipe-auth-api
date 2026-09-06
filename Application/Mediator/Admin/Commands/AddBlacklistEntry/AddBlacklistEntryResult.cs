namespace Application.Mediator.Admin.Commands.AddBlacklistEntry;

public class AddBlacklistEntryResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    public static AddBlacklistEntryResult Success() => new() { IsSuccess = true };
    public static AddBlacklistEntryResult Failure(string message) => new() { IsSuccess = false, ErrorMessage = message };
}