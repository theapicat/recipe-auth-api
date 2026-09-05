using Application;
using Application.TokenService;
using Application.TokenService.Interfaces;
using Domain.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace API.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // add built-in services
        services.AddControllers();
        
        // 1. Registrer Google OAuth
        services.AddAuthentication()
            .AddGoogle("Google", options =>
            {
                options.ClientId = configuration["AppSettings:GoogleClientId"]!;
                options.ClientSecret = configuration["AppSettings:GoogleClientSecret"]!;
                options.SignInScheme = IdentityConstants.ExternalScheme;
                
                // Oppdatert callback-sti med full /api/auth-prefiks
                options.CallbackPath = "/api/auth/account/signin-google";

                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

        services.ConfigureExternalCookie(opts =>
        {
            opts.Cookie.SameSite = SameSiteMode.Lax;
            opts.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        
        // add options
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<AdminUserOptions>(configuration.GetSection(AdminUserOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));
        services.AddScoped<ITokenService, TokenService>();
        
        
        // add minor (less configuration) services
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<ApplicationMarker>();
        });

        return services;
    }
}