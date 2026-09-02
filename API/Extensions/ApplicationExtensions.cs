using Application;
using Domain.Options;

namespace API.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // add built-in services
        services.AddControllers();
        
        // add options
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<AdminUserOptions>(configuration.GetSection(AdminUserOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));
        
        // add minor (less configuration) services
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<ApplicationMarker>();
        });

        return services;
    }
}