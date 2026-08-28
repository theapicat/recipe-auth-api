using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[ApiController]
[AllowAnonymous]
public class HealthController(IConfiguration configuration, IHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public IActionResult GetStatus()
    {
        var serviceName = configuration["Serilog:Properties:Application"] 
                          ?? environment.ApplicationName;

        return Ok(new
        {
            Status = "Healthy",
            Service = serviceName,
            Environment = environment.EnvironmentName,
            Timestamp = DateTime.UtcNow
        });
    }
}