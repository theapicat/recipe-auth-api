using API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Registrer tjenester via Extension-metodene våre
builder.Services.AddDbContext(builder.Configuration);
builder.Services.AddCustomIdentityAndOpenIddict(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();