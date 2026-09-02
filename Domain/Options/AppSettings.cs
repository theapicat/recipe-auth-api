namespace Domain.Options;

public class AppSettings
{
    public const string SectionName = "AppSettings";

    public string FrontendUrl { get; set; } = string.Empty;

    // OpenIddict Klienter
    public string WebAppClientId { get; set; } = string.Empty;
    public string WebAppDisplayName { get; set; } = string.Empty;
    public string MobileAppClientId { get; set; } = string.Empty;
    public string MobileAppDisplayName { get; set; } = string.Empty;
}