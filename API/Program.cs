using API.Extensions;
using API.Services;

var builder = WebApplication.CreateBuilder(args);

// Registrer tjenester via Extension-metodene våre
builder.Services.AddDbContext(builder.Configuration);
builder.Services.AddCustomIdentityAndOpenIddict(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHostedService<OpenIddictSeeder>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();