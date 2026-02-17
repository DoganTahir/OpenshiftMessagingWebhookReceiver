using Microsoft.AspNetCore.Mvc;

namespace OpenshiftWebHook.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet("health")]
    [IgnoreAntiforgeryToken]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "OpenshiftWebHook"
        });
    }

    [HttpGet("ready")]
    [IgnoreAntiforgeryToken]
    public IActionResult Ready()
    {
        // Add readiness checks here if needed (e.g., database connectivity, external service availability)
        return Ok(new
        {
            Status = "Ready",
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("live")]
    [IgnoreAntiforgeryToken]
    public IActionResult Live()
    {
        // Liveness probe - indicates the service is running
        return Ok(new
        {
            Status = "Alive",
            Timestamp = DateTime.UtcNow
        });
    }
}
