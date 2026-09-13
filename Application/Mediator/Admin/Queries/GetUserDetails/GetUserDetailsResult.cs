using Application.Mediator.Common;
using Domain.DTOs.Admin.Responses;

namespace Application.Mediator.Admin.Queries.GetUserDetails;

public class GetUserDetailsResult : OperationResult
{
    public AdminUserDetailsResponse? Details { get; set; }

    public static GetUserDetailsResult Success(AdminUserDetailsResponse details)
    {
        return new GetUserDetailsResult { IsSuccess = true, Details = details };
    }

    public static GetUserDetailsResult NotFound(string message = "Bruker ikke funnet.")
    {
        return new GetUserDetailsResult { IsSuccess = false, ErrorMessage = message };
    }
}
