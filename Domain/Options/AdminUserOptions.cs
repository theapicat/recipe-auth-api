namespace Domain.Options;

public class AdminUserOptions
{
    public const string SectionName = "AdminUser";

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}