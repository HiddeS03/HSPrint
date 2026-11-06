using System.Net;
using HSPrint.Services;
using Microsoft.AspNetCore.Mvc;

namespace HSPrint.Controllers;

/// <summary>
/// Koppelen met Label Genie voor printen via internet. Alleen vanaf deze pc
/// zelf (de browser of HSPrint Instellingen), niet vanaf andere pc's in het netwerk.
/// </summary>
[ApiController]
[Route("cloud")]
public class CloudController : ControllerBase
{
    private readonly CloudPrintService _cloud;

    public CloudController(CloudPrintService cloud)
    {
        _cloud = cloud;
    }

    private bool IsLocal()
    {
        var ip = HttpContext.Connection.RemoteIpAddress;
        return ip != null && IPAddress.IsLoopback(ip);
    }

    [HttpGet("status")]
    public IActionResult Status() => Ok(_cloud.Status);

    public record PairRequest(string Code);

    [HttpPost("pair")]
    public async Task<IActionResult> Pair([FromBody] PairRequest request, CancellationToken ct)
    {
        if (!IsLocal()) return StatusCode(403, new { error = "Koppelen kan alleen op deze pc zelf." });
        var (ok, error) = await _cloud.PairAsync(request.Code, ct);
        return ok ? Ok(_cloud.Status) : BadRequest(new { error });
    }

    [HttpPost("unpair")]
    public IActionResult Unpair()
    {
        if (!IsLocal()) return StatusCode(403, new { error = "Ontkoppelen kan alleen op deze pc zelf." });
        _cloud.Unpair();
        return Ok(_cloud.Status);
    }
}
