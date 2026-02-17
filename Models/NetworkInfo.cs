namespace HSPrint.Models;

public record NetworkInfo(
    string Hostname,
    string IpAddress,
    int Port,
    string Version,
    IEnumerable<PrinterInfo> Printers,
    DateTime Timestamp
);

public record PrinterInfo(
    string Name,
    bool IsDefault,
    bool IsNetworkPrinter = false
);
