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
        string? tempPdfPath = null;
        try
        {
            _logger.LogInformation("Printing PDF to printer: {PrinterName}", printerName);

            // Save PDF to temp file
            byte[] pdfBytes = Convert.FromBase64String(base64Pdf);
            tempPdfPath = Path.Combine(Path.GetTempPath(), $"hsprint_{Guid.NewGuid()}.pdf");
            await File.WriteAllBytesAsync(tempPdfPath, pdfBytes);

            // Try using SumatraPDF for silent printing (if available)
            var sumatraPaths = new[]
              {
     @"C:\Program Files\SumatraPDF\SumatraPDF.exe",
  @"C:\Program Files (x86)\SumatraPDF\SumatraPDF.exe",
  Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SumatraPDF.exe")
   };

            string? sumatraPath = sumatraPaths.FirstOrDefault(File.Exists);

            if (sumatraPath != null)
            {
                // Use SumatraPDF for reliable printing
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = sumatraPath,
                    Arguments = $"-print-to \"{printerName}\" -silent \"{tempPdfPath}\"",
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    UseShellExecute = false
                };

                var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();

                    await Task.Delay(2000); // Give time for spooler

                    _logger.LogInformation("Successfully sent PDF to {PrinterName} using SumatraPDF", printerName);
                }
            }
            else
            {
                // Fallback: Try Adobe Acrobat Reader command line
                var adobePaths = new[]
                  {
   @"C:\Program Files\Adobe\Acrobat DC\Acrobat\Acrobat.exe",
          @"C:\Program Files (x86)\Adobe\Acrobat Reader DC\Reader\AcroRd32.exe",
            @"C:\Program Files\Adobe\Acrobat Reader DC\Reader\AcroRd32.exe"
           };

                string? adobePath = adobePaths.FirstOrDefault(File.Exists);

                if (adobePath != null)
                {
                    var processInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = adobePath,
                        Arguments = $"/t \"{tempPdfPath}\" \"{printerName}\"",
                        CreateNoWindow = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                        UseShellExecute = false
                    };

                    var process = System.Diagnostics.Process.Start(processInfo);
                    if (process != null)
                    {
                        await Task.Delay(5000); // Wait for Adobe to send to spooler

                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill(true);
                            }
                        }
                        catch
                        {
                            // Ignore errors killing Adobe Reader
                        }

                        _logger.LogInformation("Successfully sent PDF to {PrinterName} using Adobe Reader", printerName);
                    }
                }
                else
                {
                    // Last resort: Windows default verb (unreliable but better than nothing)
                    _logger.LogWarning("No PDF printer utility found (SumatraPDF or Adobe Reader). Using Windows shell - this may not work reliably.");
                    _logger.LogWarning("For best results, install SumatraPDF from https://www.sumatrapdfreader.org/");

                    var processInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c start /min \"\" \"{tempPdfPath}\"",
                        CreateNoWindow = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                        UseShellExecute = false
                    };

                    System.Diagnostics.Process.Start(processInfo);
                    await Task.Delay(3000);

                    _logger.LogWarning("Attempted to print PDF using Windows shell to {PrinterName}", printerName);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing PDF to {PrinterName}", printerName);
            return false;
        }
        finally
        {
            // Cleanup temp file after delay
            if (tempPdfPath != null)
            {
                _ = Task.Run(async () =>
                    {
                        await Task.Delay(10000); // Wait 10 seconds
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
                    });
            }
        }
    }
}
