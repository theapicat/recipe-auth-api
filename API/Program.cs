using API.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Persistence.Seeders;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Registrer tjenester via Extension-metodene våre
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddDbContext(builder.Configuration);
builder.Services.AddCustomIdentityAndOpenIddict(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHostedService<OpenIddictSeeder>();
builder.Services.AddMassTransitServices(builder.Configuration);
builder.Services.AddQuartzJobs(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders(); 

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await IdentitySeeder.SeedAsync(app.Services);

app.Logger.LogInformation("🚀 Applikasjonen har startet og lytter på forespørsler!");
app.Run();