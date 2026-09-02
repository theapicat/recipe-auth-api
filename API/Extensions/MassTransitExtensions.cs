using MassTransit;

namespace API.Extensions;

public static class MassTransitExtensions
{
    public static IServiceCollection AddMassTransitServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RabbitMQ:Host"] ?? "localhost";
                var port = ushort.Parse(configuration["RabbitMQ:Port"] ?? "5672");
                var virtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/";
                var username = configuration["RabbitMQ:Username"] ?? "rabbit_user";
                var password = configuration["RabbitMQ:Password"] ?? "rabbit_secure_password_dev";

                cfg.Host(host, port, virtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}