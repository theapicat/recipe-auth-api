using System.Text;
using API.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace API.Extensions;

public static class OpenIddictExtensions
{
    public static IServiceCollection AddCustomIdentityAndOpenIddict(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. ASP.NET Core Identity
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

        // 2. Secret Key for JWT-signering (må matche Gateway sin JWT__KEY)
        var jwtKey = configuration["JWT:SecretKey"]
            ?? throw new InvalidOperationException("JWT:SecretKey is missing");

        // 3. OpenIddict Core & Server
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();
            })
            .AddServer(options =>
            {
                // OAuth2 / OIDC Endepunkter
                options.SetTokenEndpointUris("/connect/token");

                // Flows som støttes
                options.AllowPasswordFlow()
                       .AllowRefreshTokenFlow();

                // Symmetrisk nøkkel for deling med Gateway
                options.AddSigningKey(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)));

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