namespace Domain.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;

    public string Key
    {
        get => SecretKey;
        set => SecretKey = value;
    }

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    // 💡 Nye felter for konfigurerbar levetid:
    public int AccessTokenLifetimeInMinutes { get; set; } = 60; // Standard: 60 minutter
    public int RefreshTokenLifetimeInDays { get; set; } = 14;  // Standard: 14 dager
}