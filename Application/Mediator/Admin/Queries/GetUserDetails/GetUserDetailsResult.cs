using Domain.DTOs.Admin;

namespace Application.Mediator.Admin.Queries.GetUserDetails;

public class GetUserDetailsResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public AdminUserDetailsDto? Details { get; set; }

    public static GetUserDetailsResult Success(AdminUserDetailsDto details) => new() { IsSuccess = true, Details = details };
    public static GetUserDetailsResult NotFound(string message = "Bruker ikke funnet.") => new() { IsSuccess = false, ErrorMessage = message };
}