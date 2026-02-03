namespace HSPrint.Models;

public record NetworkPrintRequest(
    string TargetIp,
    int TargetPort,
    string PrinterName,
    string PrintType,
    string Data
);

public record NetworkPrintResponse(
    bool Success,
    string? Message,
    string? Error
);
