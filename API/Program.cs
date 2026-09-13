using API.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Persistence.Seeders;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Registrer tjenester via Extension-metodene våre
builder.Services.AddApplicationServices(builder.Configuration);

// PostgreSQL (via AddDbContext), MassTransit (RabbitMQ) og Quartz krever ekte infrastruktur
// som ikke finnes i test-verten som WebApplicationFactory<Program> starter opp
// (se Tests/Support/AuthApiWebApplicationFactory.cs, som registrerer ApplicationDbContext
// mot SQLite in-memory i stedet). Dette er den eneste forskjellen i oppstart for
// "Testing"-miljøet - alt annet, inkludert Identity/OpenIddict-oppsettet, kjører likt.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext(builder.Configuration);
    builder.Services.AddMassTransitServices(builder.Configuration);
    builder.Services.AddQuartzJobs(builder.Configuration);
}

builder.Services.AddCustomIdentityAndOpenIddict(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHostedService<OpenIddictSeeder>();

var app = builder.Build();

app.UseForwardedHeaders();

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await IdentitySeeder.SeedAsync(app.Services);

app.Logger.LogInformation("🚀 Applikasjonen har startet og lytter på forespørsler!");
app.Run();

// Gjør Program-klassen som top-level statements genererer synlig for
// WebApplicationFactory<Program> i Tests-prosjektet.
public partial class Program;