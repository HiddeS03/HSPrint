using HSPrint.Models;

namespace HSPrint.Services;

public interface IPrintService
{
    Task<(bool success, string? errorMessage)> PrintZpl(string printerName, string zpl);
    Task<bool> PrintZplViaTcp(string ip, int port, string zpl);
    Task<bool> PrintImage(string printerName, string base64Png);
    Task<bool> PrintPdf(string printerName, string base64Pdf);
    List<PrinterInfo> GetAvailablePrinters();
}
