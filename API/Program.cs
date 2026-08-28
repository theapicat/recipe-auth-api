using API.Extensions;
using API.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();

// Registrer tjenester via Extension-metodene våre
builder.Services.AddDbContext(builder.Configuration);
builder.Services.AddCustomIdentityAndOpenIddict(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHostedService<OpenIddictSeeder>();

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await IdentitySeeder.SeedAsync(app.Services);

app.Logger.LogInformation("🚀 Applikasjonen har startet og lytter på forespørsler!");
app.Run();