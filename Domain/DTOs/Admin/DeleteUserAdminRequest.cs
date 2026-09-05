namespace Domain.DTOs.Admin;

public record DeleteUserAdminRequest
{
    public required string UserId { get; init; }
}