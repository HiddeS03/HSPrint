using System.Drawing.Printing;

namespace HSPrint.Services;

public class PrinterService : IPrinterService
{
    private readonly ILogger<PrinterService> _logger;

    public PrinterService(ILogger<PrinterService> logger)
    {
        _logger = logger;
    }

    public IEnumerable<string> ListInstalledPrinters()
    {
        try
        {
            _logger.LogInformation("Fetching installed printers");
            var printers = new List<string>();

            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printer);
            }

            _logger.LogInformation("Found {Count} printers", printers.Count);
            return printers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching installed printers");
            return Enumerable.Empty<string>();
        }
    }
}
