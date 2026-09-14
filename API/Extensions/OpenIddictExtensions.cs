using System.Text;
using Domain.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using Persistence.Context;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace API.Extensions;

public static class OpenIddictExtensions
{
    public static IServiceCollection AddCustomIdentityAndOpenIddict(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() 
                         ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
            throw new InvalidOperationException(
                "Konfigurasjon for 'JWT:SecretKey' mangler eller er tom i appsettings.");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

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

        // 2. OpenIddict Core & Server
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<ApplicationDbContext>()
                    .ReplaceDefaultEntities<Guid>();
            })
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/api/auth/connect/token");
                options.SetRevocationEndpointUris("/api/auth/connect/revoke");

                options.AllowPasswordFlow()
                    .AllowRefreshTokenFlow();

                // 💡 TVING OPENIDDICT TIL Å BRUKE DE KONFIGURERTE LEVETIDENE:
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(jwtOptions.AccessTokenLifetimeInMinutes));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(jwtOptions.RefreshTokenLifetimeInDays));

                options.AddSigningKey(signingKey);
                options.DisableAccessTokenEncryption();

                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough()
                    .DisableTransportSecurityRequirement();

                // TokenService.IssueTokenPairAsync (Google-callback-flyten) dispatcher en
                // ProcessSignInContext direkte via IOpenIddictServerDispatcher, uten en levende
                // HTTP-forespørsel. OpenIddicts ASP.NET Core-integrasjon skriver normalt selve
                // token-responsen til HttpContext her, men den handleren er filtrert til kun å
                // kjøre når en HTTP-forespørsel faktisk er tilknyttet transaksjonen — noe vår
                // syntetiske transaksjon aldri har. Uten en fallback kaster OpenIddict "The token
                // response was not correctly applied" (nøyaktig slik unntaksmeldingen selv ber om
                // å bli løst — se ApplyTokenResponse<TContext> i OpenIddict.Server). Kjøres sist
                // (int.MaxValue) og rører ikke ekte HTTP-forespørsler, som allerede er markert
                // håndtert av ASP.NET Core-handleren innen denne når frem.
                options.AddEventHandler<ApplyTokenResponseContext>(builder => builder
                    .UseInlineHandler(context =>
                    {
                        if (!context.IsRequestHandled && !context.IsRequestSkipped)
                            context.HandleRequest();

                        return default;
                    })
                    .SetOrder(int.MaxValue));
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();

                options.Configure(valOptions =>
                {
                    valOptions.TokenValidationParameters.ValidateIssuer = true;
                    valOptions.TokenValidationParameters.ValidIssuer = jwtOptions.Issuer;
                    valOptions.TokenValidationParameters.ValidateAudience =
                        !string.IsNullOrWhiteSpace(jwtOptions.Audience);
                    valOptions.TokenValidationParameters.ValidAudience = jwtOptions.Audience;
                    valOptions.TokenValidationParameters.ValidateIssuerSigningKey = true;
                    valOptions.TokenValidationParameters.IssuerSigningKey = signingKey;
                    valOptions.TokenValidationParameters.ValidateLifetime = true;
                });
            });

        return services;
    }
}