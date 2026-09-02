using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Tilkoblingsstrengen 'DefaultConnection' mangler eller er tom i konfigurasjonen.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseOpenIddict<Guid>();
        });

        return services;
    }
}