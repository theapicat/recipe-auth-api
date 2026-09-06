using Domain.Enums;

namespace Domain.Entities;

public class BlacklistedEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // Inneholder enten e-post ("user@bad.com") eller domene ("disposable.com")
    public string Pattern { get; set; } = string.Empty;
    
    public BlacklistType Type { get; set; }
    
    public string? Reason { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid CreatedByAdminId { get; set; }
}