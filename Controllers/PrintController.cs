using Microsoft.AspNetCore.Mvc;
using HSPrint.Services;

namespace HSPrint.Controllers;

[ApiController]
[Route("[controller]")]
public class PrintController : ControllerBase
{
    private readonly IPrintService _printService;
    private readonly ILogger<PrintController> _logger;

    public PrintController(IPrintService printService, ILogger<PrintController> logger)
    {
        _printService = printService;
        _logger = logger;
    }

    /// <summary>
    /// Print ZPL to a local printer
    /// </summary>
    [HttpPost("zpl")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PrintZpl([FromBody] ZplPrintRequest request)
    {
        _logger.LogInformation("POST /print/zpl - Printer: {Printer}", request.PrinterName);

        if (string.IsNullOrWhiteSpace(request.PrinterName) || string.IsNullOrWhiteSpace(request.Zpl))
        {
            return BadRequest(new { error = "PrinterName and Zpl are required" });
        }

        var (success, errorMessage) = await _printService.PrintZpl(request.PrinterName, request.Zpl);

        if (success)
        {
            return Ok(new { message = "Print job sent successfully", printer = request.PrinterName });
        }

        return BadRequest(new { error = "Failed to send print job", details = errorMessage });
    }

    /// <summary>
    /// Print ZPL via TCP/IP to a network printer
    /// </summary>
    [HttpPost("zpl/tcp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PrintZplViaTcp([FromBody] ZplTcpPrintRequest request)
    {
        _logger.LogInformation("POST /print/zpl/tcp - IP: {IP}:{Port}", request.Ip, request.Port);

        if (string.IsNullOrWhiteSpace(request.Ip) || request.Port <= 0 || string.IsNullOrWhiteSpace(request.Zpl))
        {
            return BadRequest(new { error = "IP, Port, and Zpl are required" });
        }

        var success = await _printService.PrintZplViaTcp(request.Ip, request.Port, request.Zpl);

        if (success)
        {
            return Ok(new { message = "Print job sent successfully via TCP", ip = request.Ip, port = request.Port });
        }

        return BadRequest(new { error = "Failed to send print job via TCP" });
    }

    /// <summary>
    /// Print image (PNG) to a local printer
    /// </summary>
    [HttpPost("image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PrintImage([FromBody] ImagePrintRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PrinterName) || string.IsNullOrWhiteSpace(request.Base64Png))
        {
            return BadRequest(new { error = "PrinterName and Base64Png are required" });
        }

        var success = await _printService.PrintImage(request.PrinterName, request.Base64Png);

        if (success)
        {
            return Ok(new { message = "Image print job sent successfully", printer = request.PrinterName });
        }

        return BadRequest(new { error = "Failed to send image print job" });
    }

    /// <summary>
    /// Print PDF to a local printer
    /// </summary>
    [HttpPost("pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> PrintPdf([FromBody] PdfPrintRequest request)
    {
        _logger.LogInformation("POST /print/pdf - Printer: {Printer}", request.PrinterName);

        if (string.IsNullOrWhiteSpace(request.PrinterName) || string.IsNullOrWhiteSpace(request.Base64Pdf))
        {
            return BadRequest(new { error = "PrinterName and Base64Pdf are required" });
        }

        var success = await _printService.PrintPdf(request.PrinterName, request.Base64Pdf);

        if (success)
        {
            return Ok(new { message = "PDF print job sent successfully", printer = request.PrinterName });
        }

        return BadRequest(new { error = "Failed to send PDF print job" });
    }
}

// Request models
public record ZplPrintRequest(string PrinterName, string Zpl);
public record ZplTcpPrintRequest(string Ip, int Port, string Zpl);
public record ImagePrintRequest(string PrinterName, string Base64Png);
public record PdfPrintRequest(string PrinterName, string Base64Pdf);
