namespace Domain.Options;

public class JwtOptions
{
    public const string SectionName = "JWT";

    public string SecretKey { get; set; } = string.Empty;
}