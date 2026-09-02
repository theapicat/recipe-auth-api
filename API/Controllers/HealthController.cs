using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth/health")]
public class HealthController(IHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = environment.ApplicationName,
            Environment = environment.EnvironmentName,
            Timestamp = DateTime.UtcNow
        });
    }
}