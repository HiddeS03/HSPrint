using System.Drawing;
using System.Drawing.Printing;
using System.Net.Sockets;
using System.Text;
using HSPrint.Utils;

namespace HSPrint.Services;

public class PrintService : IPrintService
{
  private readonly ILogger<PrintService> _logger;

    public PrintService(ILogger<PrintService> logger)
    {
  _logger = logger;
    }

    public async Task<bool> PrintZpl(string printerName, string zpl)
    {
        try
        {
            _logger.LogInformation("Printing ZPL to printer: {PrinterName}", printerName);
            
            bool success = RawPrinterHelper.SendStringToPrinter(printerName, zpl);
            
     if (success)
       {
_logger.LogInformation("Successfully printed ZPL to {PrinterName}", printerName);
            }
   else
            {
      _logger.LogWarning("Failed to print ZPL to {PrinterName}", printerName);
      }
            
            return await Task.FromResult(success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing ZPL to {PrinterName}", printerName);
    return false;
        }
  }

    public async Task<bool> PrintZplViaTcp(string ip, int port, string zpl)
    {
        try
        {
    _logger.LogInformation("Printing ZPL via TCP to {IP}:{Port}", ip, port);
   
      using var client = new TcpClient();
            await client.ConnectAsync(ip, port);
            
    using var stream = client.GetStream();
   byte[] data = Encoding.UTF8.GetBytes(zpl);
          await stream.WriteAsync(data, 0, data.Length);
       
        _logger.LogInformation("Successfully sent ZPL via TCP to {IP}:{Port}", ip, port);
            return true;
        }
    catch (Exception ex)
{
            _logger.LogError(ex, "Error printing ZPL via TCP to {IP}:{Port}", ip, port);
      return false;
        }
    }

    public async Task<bool> PrintImage(string printerName, string base64Png)
    {
        try
        {
          _logger.LogInformation("Printing image to printer: {PrinterName}", printerName);
            
       byte[] imageBytes = Convert.FromBase64String(base64Png);
          using var ms = new MemoryStream(imageBytes);
         using var image = Image.FromStream(ms);
         
var printDocument = new PrintDocument();
            printDocument.PrinterSettings.PrinterName = printerName;
     
            printDocument.PrintPage += (sender, e) =>
            {
if (e.Graphics != null)
         {
       e.Graphics.DrawImage(image, e.PageBounds);
            }
       };
        
            printDocument.Print();
 
        _logger.LogInformation("Successfully printed image to {PrinterName}", printerName);
  return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
  _logger.LogError(ex, "Error printing image to {PrinterName}", printerName);
            return false;
        }
  }

    public async Task<bool> PrintPdf(string printerName, string base64Pdf)
    {
        try
        {
            _logger.LogInformation("Printing PDF to printer: {PrinterName}", printerName);
            
    // Save PDF to temp file
   byte[] pdfBytes = Convert.FromBase64String(base64Pdf);
            string tempPdfPath = Path.Combine(Path.GetTempPath(), $"hsprint_{Guid.NewGuid()}.pdf");
            await File.WriteAllBytesAsync(tempPdfPath, pdfBytes);
            
         // Use shell command to print PDF
     var processInfo = new System.Diagnostics.ProcessStartInfo
            {
   FileName = tempPdfPath,
   Verb = "printto",
       Arguments = $"\"{printerName}\"",
         CreateNoWindow = true,
     WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
      };
            
         var process = System.Diagnostics.Process.Start(processInfo);
      
       // Wait a bit and cleanup
            await Task.Delay(5000);
        
     try
   {
    if (File.Exists(tempPdfPath))
  {
        File.Delete(tempPdfPath);
    }
    }
   catch
       {
     // Ignore cleanup errors
    }
   
          _logger.LogInformation("Successfully sent PDF to {PrinterName}", printerName);
            return true;
        }
        catch (Exception ex)
        {
   _logger.LogError(ex, "Error printing PDF to {PrinterName}", printerName);
    return false;
        }
    }
}
