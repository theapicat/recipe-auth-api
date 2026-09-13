using Application.Mediator.Common;

namespace Application.Mediator.Admin.Commands.AddBlacklistEntry;

public class AddBlacklistEntryResult : OperationResult
{
    public static AddBlacklistEntryResult Success()
    {
        return new AddBlacklistEntryResult { IsSuccess = true };
    }

    public static AddBlacklistEntryResult Failure(string message)
    {
        return new AddBlacklistEntryResult { IsSuccess = false, ErrorMessage = message };
    }
}
