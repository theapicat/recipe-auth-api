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
        // 1. Registrer og hent JwtOptions fra appsettings.json
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

        if (string.IsNullOrWhiteSpace(jwtOptions?.SecretKey))
        {
            throw new InvalidOperationException("Konfigurasjon for 'JWT:SecretKey' mangler eller er tom i appsettings.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

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
                options.SetTokenEndpointUris("/api/auth/connect/token");

                // Flows som støttes
                options.AllowPasswordFlow()
                       .AllowRefreshTokenFlow();

                // Symmetrisk nøkkel for deling med Gateway og TokenService
                options.AddSigningKey(signingKey);

                // Tving ukrypterte JWT-er slik at TokenService sine tokens kan leses
                options.DisableAccessTokenEncryption();

                // Utviklingssertifikater
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                // ASP.NET Core MVC-passthrough
                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough()
                       .DisableTransportSecurityRequirement();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();

                // Konfigurer valideringen til å godta "recipe-auth-app" fra appsettings.json
                options.Configure(valOptions =>
                {
                    valOptions.TokenValidationParameters.ValidateIssuer = true;
                    valOptions.TokenValidationParameters.ValidIssuer = jwtOptions.Issuer; // "recipe-auth-app"
                    valOptions.TokenValidationParameters.ValidateAudience = !string.IsNullOrWhiteSpace(jwtOptions.Audience);
                    valOptions.TokenValidationParameters.ValidAudience = jwtOptions.Audience; // "recipe-frontend"
                    valOptions.TokenValidationParameters.ValidateIssuerSigningKey = true;
                    valOptions.TokenValidationParameters.IssuerSigningKey = signingKey;
                    valOptions.TokenValidationParameters.ValidateLifetime = true;
                });
            });

        return services;
    }
}