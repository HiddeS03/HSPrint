using System.Runtime.InteropServices;
using System.Text;

namespace HSPrint.Utils;

public class RawPrinterHelper
{
    // Structure and API declarations for raw printer access
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class DOCINFOA
{
  [MarshalAs(UnmanagedType.LPStr)]
        public string? pDocName;
        [MarshalAs(UnmanagedType.LPStr)]
 public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)]
     public string? pDataType;
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool StartDocPrinter(IntPtr hPrinter, Int32 level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

[DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    public static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
  public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, Int32 dwCount, out Int32 dwWritten);

    /// <summary>
    /// Sends bytes to a printer
    /// </summary>
    public static bool SendBytesToPrinter(string printerName, IntPtr pBytes, int dwCount)
    {
        IntPtr hPrinter = IntPtr.Zero;
        DOCINFOA di = new DOCINFOA();
  bool success = false;

        di.pDocName = "HSPrint Document";
        di.pDataType = "RAW";

     try
     {
        // Open the printer
       if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
        {
     // Start a document
   if (StartDocPrinter(hPrinter, 1, di))
      {
          // Start a page
     if (StartPagePrinter(hPrinter))
               {
          // Write bytes to the printer
      int written;
       success = WritePrinter(hPrinter, pBytes, dwCount, out written);
    EndPagePrinter(hPrinter);
          }
         EndDocPrinter(hPrinter);
 }
           ClosePrinter(hPrinter);
}
        }
   catch
        {
        success = false;
        }

        return success;
    }

    /// <summary>
 /// Sends a string to a printer
    /// </summary>
    public static bool SendStringToPrinter(string printerName, string data)
    {
        IntPtr pBytes = IntPtr.Zero;
        bool success = false;

        try
 {
            // Convert the string to bytes
 byte[] bytes = Encoding.UTF8.GetBytes(data);
            int count = bytes.Length;

      // Allocate unmanaged memory for the bytes
  pBytes = Marshal.AllocCoTaskMem(count);
            Marshal.Copy(bytes, 0, pBytes, count);

            // Send to printer
            success = SendBytesToPrinter(printerName, pBytes, count);
        }
 finally
        {
          // Free the unmanaged memory
   if (pBytes != IntPtr.Zero)
  {
     Marshal.FreeCoTaskMem(pBytes);
}
      }

     return success;
    }
}
