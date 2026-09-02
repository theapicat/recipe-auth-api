using Domain.Options;
using MassTransit;
using Microsoft.Extensions.Options;

namespace API.Extensions;

public static class MassTransitExtensions
{
    public static IServiceCollection AddMassTransitServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Sikrer at RabbitMqOptions er registrert
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                // Henter sterk type fra DI
                var rabbitOptions = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                cfg.Host(rabbitOptions.Host, (ushort)rabbitOptions.Port, rabbitOptions.VirtualHost, h =>
                {
                    h.Username(rabbitOptions.Username);
                    h.Password(rabbitOptions.Password);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}