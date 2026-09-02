using System.Text;
using Domain.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Persistence.Context;

namespace API.Extensions;

public static class OpenIddictExtensions
{
    public static IServiceCollection AddCustomIdentityAndOpenIddict(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Registrer og hent JwtOptions
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

        if (string.IsNullOrWhiteSpace(jwtOptions?.SecretKey))
        {
            throw new InvalidOperationException("Konfigurasjon for 'JWT:SecretKey' mangler eller er tom i appsettings.");
        }

        // 2. ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // 3. OpenIddict Core & Server
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<ApplicationDbContext>()
                    .ReplaceDefaultEntities<Guid>();
            })
            .AddServer(options =>
            {
                // OAuth2 / OIDC Endepunkter
                options.SetTokenEndpointUris("/connect/token");

                // Flows som støttes
                options.AllowPasswordFlow()
                       .AllowRefreshTokenFlow();

                // Symmetrisk nøkkel for deling med Gateway
                options.AddSigningKey(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)));

                // Utviklingssertifikater
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                // ASP.NET Core MVC-passthrough
                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough()
                       .DisableTransportSecurityRequirement(); // Tillat HTTP under lokal dev
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}