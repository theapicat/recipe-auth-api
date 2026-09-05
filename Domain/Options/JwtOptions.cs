namespace Domain.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    
    // Støtter også "Key" hvis det står det i appsettings.json
    public string Key 
    { 
        get => SecretKey; 
        set => SecretKey = value; 
    }

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}