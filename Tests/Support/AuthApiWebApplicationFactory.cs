using API.Consumers;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;

namespace Tests.Support;

/// <summary>
/// Lag 4 (API/OpenIddict-integrasjonstester): starter hele Program.cs sin faktiske
/// oppstartspipeline (Identity, OpenIddict, seedere, controllere) mot en SQLite in-memory
/// database, i "Testing"-miljøet der MassTransit/Quartz bevisst er koblet ut i Program.cs
/// (de krever ekte RabbitMQ som ikke finnes i testmiljøet).
///
/// IdentitySeeder og OpenIddictSeeder kjører som normalt ved oppstart, så
/// admin-kontoen (AdminUser-config) og OAuth2-klientene ("recipe-web-app" m.fl.)
/// finnes allerede når testene starter - ingen egen seeding-logikk trengs her.
///
/// Del én instans per testklasse (IClassFixture), ikke per test - full host-oppstart
/// er for tregt å gjøre om og om igjen. Bruk unike e-poster per test for å unngå kollisjon.
/// </summary>
public class AuthApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public AuthApiWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Program.cs sitt IdentitySeeder.SeedAsync-kall kjører FØR OpenIddictSeeder (en
        // IHostedService som ellers ville opprettet skjemaet via EnsureCreatedAsync når
        // verten faktisk starter). I produksjon finnes skjemaet allerede via ekte EF-migreringer
        // mot Postgres, så det er aldri et problem der - men i test-verten må vi opprette
        // skjemaet FØR noe av Program.cs sin oppstartslogikk i det hele tatt kjører.
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite(_connection);
        optionsBuilder.UseOpenIddict<Guid>();
        using var context = new ApplicationDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // AddGoogle sitt OAuthOptions.Validate() kaster hardt dersom ClientId/ClientSecret er
        // tomme (leses fra AppSettings:GoogleClientId/-Secret, se ApplicationExtensions.cs), og
        // valideres allerede ved første forespørsel via UseAuthentication() - selv på anonyme
        // endepunkter. Ingen faktisk Google-innlogging testes her, så dummyverdier holder.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:GoogleClientId"] = "test-google-client-id",
                ["AppSettings:GoogleClientSecret"] = "test-google-client-secret"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Program.cs hopper bevisst over den ekte Npgsql-registreringen i "Testing"-miljøet,
            // så dette er den eneste registreringen av ApplicationDbContext i test-verten.
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.UseOpenIddict<Guid>();
            });

            // Program.cs hopper over den ekte RabbitMQ-registreringen i "Testing"-miljøet, så
            // uten dette ville enhver handler som tar inn IPublishEndpoint (Register, admin-
            // handlinger m.fl.) feile med en DI-resolve-feil. Gir et fullverdig in-memory
            // meldingsbuss uten ekstern broker - anbefalt fremgangsmåte fra MassTransit selv.
            services.AddMassTransitTestHarness(cfg => { cfg.AddConsumer<InvalidEmailDetectedConsumer>(); });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
