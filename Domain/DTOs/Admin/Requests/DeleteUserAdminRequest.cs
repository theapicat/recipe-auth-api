namespace Domain.DTOs.Admin.Requests;

public record DeleteUserAdminRequest
{
    public required string UserId { get; init; }
}