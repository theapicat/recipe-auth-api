using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    // DbSet for svarteliste
    public DbSet<BlacklistedEntry> BlacklistedEntries => Set<BlacklistedEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.UseOpenIddict<Guid>();

        // Konfigurasjon for BlacklistedEntry
        builder.Entity<BlacklistedEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Pattern).IsRequired().HasMaxLength(256);
            
            // Indeks for superraske oppslag ved registrering
            entity.HasIndex(e => new { e.Pattern, e.Type });
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<ApplicationUser>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.LastModifiedAt = DateTime.UtcNow;
        }
    }
}