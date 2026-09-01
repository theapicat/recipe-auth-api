using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Missing connection string");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseOpenIddict<Guid>();
        });

        return services;
    }
}