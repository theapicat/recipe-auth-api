namespace Domain.DTOs.Admin;

public record SendUserEmailAdminRequest(
    string UserId,
    string Subject,
    string Message
);