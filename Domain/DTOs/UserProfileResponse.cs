namespace Domain.DTOs;

public class UserProfileResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // Google- & Passord-flagg
    public bool HasPassword { get; set; }
    public bool IsGoogleAccount { get; set; }

    // Status & Metadata for Frontend
    public bool IsEmailConfirmed { get; set; }
    public bool WelcomeCompleted { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}