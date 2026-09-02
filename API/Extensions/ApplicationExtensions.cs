using Application;

namespace API.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<ApplicationMarker>();
        });

        return services;
    }
}