using Microsoft.AspNetCore.Mvc;
using HSPrint.Services;

namespace HSPrint.Controllers;

[ApiController]
[Route("[controller]")]
public class PrinterController : ControllerBase
{
    private readonly IPrinterService _printerService;
    private readonly ILogger<PrinterController> _logger;

    public PrinterController(IPrinterService printerService, ILogger<PrinterController> logger)
    {
   _printerService = printerService;
    _logger = logger;
    }

    /// <summary>
    /// Get list of installed printers
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<string>> GetPrinters()
    {
        _logger.LogInformation("GET /printer - Fetching installed printers");
        var printers = _printerService.ListInstalledPrinters();
        return Ok(printers);
    }
}
