namespace Domain.DTOs.Admin.Requests;

public record SendUserEmailAdminRequest(
    string UserId,
    string Subject,
    string Message
);