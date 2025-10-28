using Microsoft.AspNetCore.Mvc;
using HSPrint.Utils;

namespace HSPrint.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly string _version;

    public HealthController(ILogger<HealthController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _version = configuration["Version"] ?? "1.0.0";
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
  public ActionResult GetHealth()
    {
  _logger.LogInformation("GET /health - Health check");
        var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
 
        return Ok(new
  {
       status = "ok",
      pid = processId,
   version = _version,
        timestamp = DateTime.UtcNow
     });
    }

    /// <summary>
    /// Get application version
    /// </summary>
    [HttpGet("version")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult GetVersion()
    {
        _logger.LogInformation("GET /health/version - Version check");
     
   return Ok(new
 {
            version = _version,
     timestamp = DateTime.UtcNow
     });
    }

    /// <summary>
    /// Check for available updates
    /// </summary>
    [HttpGet("update")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> CheckUpdate([FromServices] Updater updater)
    {
  _logger.LogInformation("GET /health/update - Update check");
  
var updateInfo = await updater.CheckForUpdates();
   
   if (updateInfo != null)
        {
   return Ok(new
   {
             updateAvailable = true,
currentVersion = _version,
      newVersion = updateInfo.Version,
   downloadUrl = updateInfo.DownloadUrl,
      releaseNotes = updateInfo.ReleaseNotes
       });
        }

        return Ok(new
  {
       updateAvailable = false,
    currentVersion = _version
        });
    }
}
