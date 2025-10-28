namespace HSPrint.Services;

public interface IPrinterService
{
    IEnumerable<string> ListInstalledPrinters();
}
