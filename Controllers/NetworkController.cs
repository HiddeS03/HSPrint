using Microsoft.AspNetCore.Mvc;
using HSPrint.Services;
using HSPrint.Models;
using System.Net;
using System.Net.Sockets;

namespace HSPrint.Controllers;

[ApiController]
[Route("[controller]")]
public class NetworkController : ControllerBase
{
    private readonly IPrinterService _printerService;
    private readonly INetworkService _networkService;
    private readonly ILogger<NetworkController> _logger;
    private readonly IConfiguration _configuration;

    public NetworkController(
        IPrinterService printerService,
        INetworkService networkService,
        ILogger<NetworkController> logger,
        IConfiguration configuration)
    {
        _printerService = printerService;
        _networkService = networkService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get network information about this HSPrint instance
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<NetworkInfo> GetNetworkInfo()
    {
        _logger.LogInformation("GET /network/info - Fetching network information");

        try
        {
            var hostname = Dns.GetHostName();
            var ipAddress = GetLocalIPAddress();
            var port = _configuration.GetValue<int>("Port", 5246);
            var version = _configuration.GetValue<string>("Version", "1.0.0") ?? "1.0.0";

            var printers = _printerService.ListInstalledPrinters()
                .Select(p => new PrinterInfo(p, false))
                .ToList();

            var networkInfo = new NetworkInfo(
                Hostname: hostname,
                IpAddress: ipAddress,
                Port: port,
                Version: version,
                Printers: printers,
                Timestamp: DateTime.UtcNow
            );

            return Ok(networkInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting network info");
            return StatusCode(500, new { error = "Failed to get network information" });
        }
    }

    /// <summary>
    /// Get information from a remote HSPrint instance
    /// </summary>
    [HttpGet("remote/info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NetworkInfo>> GetRemoteInfo([FromQuery] string ip, [FromQuery] int port = 5246)
    {
        _logger.LogInformation("GET /network/remote/info - IP: {IP}:{Port}", ip, port);

        if (string.IsNullOrWhiteSpace(ip))
        {
            return BadRequest(new { error = "IP address is required" });
        }

        var info = await _networkService.GetRemoteNodeInfo(ip, port);

        if (info != null)
        {
            return Ok(info);
        }

        return NotFound(new { error = $"Could not connect to {ip}:{port}" });
    }

    /// <summary>
    /// Forward a print job to a remote HSPrint instance
    /// </summary>
    [HttpPost("print")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NetworkPrintResponse>> ForwardPrintJob([FromBody] NetworkPrintRequest request)
    {
        _logger.LogInformation("POST /network/print - Forwarding to {IP}:{Port}, printer: {Printer}, type: {Type}",
            request.TargetIp, request.TargetPort, request.PrinterName, request.PrintType);

        if (string.IsNullOrWhiteSpace(request.TargetIp))
        {
            return BadRequest(new NetworkPrintResponse(false, null, "TargetIp is required"));
        }

        if (string.IsNullOrWhiteSpace(request.PrinterName))
        {
            return BadRequest(new NetworkPrintResponse(false, null, "PrinterName is required"));
        }

        if (string.IsNullOrWhiteSpace(request.PrintType))
        {
            return BadRequest(new NetworkPrintResponse(false, null, "PrintType is required (zpl, image, pdf)"));
        }

        if (string.IsNullOrWhiteSpace(request.Data))
        {
            return BadRequest(new NetworkPrintResponse(false, null, "Data is required"));
        }

        var success = await _networkService.PrintToRemoteNode(
            request.TargetIp,
            request.TargetPort,
            request.PrinterName,
            request.PrintType,
            request.Data
        );

        if (success)
        {
            return Ok(new NetworkPrintResponse(
                Success: true,
                Message: $"Print job successfully forwarded to {request.TargetIp}:{request.TargetPort}",
                Error: null
            ));
        }

        return BadRequest(new NetworkPrintResponse(
            Success: false,
            Message: null,
            Error: $"Failed to forward print job to {request.TargetIp}:{request.TargetPort}"
        ));
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            
            // Prioritize private network IP addresses (192.168.x.x, 10.x.x.x, 172.16-31.x.x)
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    var ipString = ip.ToString();
                    var bytes = ip.GetAddressBytes();
                    
                    // Check for private network ranges
                    if (bytes[0] == 192 && bytes[1] == 168) // 192.168.x.x
                    {
                        return ipString;
                    }
                    if (bytes[0] == 10) // 10.x.x.x
                    {
                        return ipString;
                    }
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) // 172.16-31.x.x
                    {
                        return ipString;
                    }
                }
            }
            
            // If no private IP found, return any IPv4 address that's not loopback
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
            
            return "127.0.0.1";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not determine local IP address");
            return "127.0.0.1";
        }
    }
}
