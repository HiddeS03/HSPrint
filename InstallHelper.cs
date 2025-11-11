using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace HSPrint.Utils;

/// <summary>
/// Helper class to manage HSPrint installation and updates
/// </summary>
public static class InstallHelper
{
    private const string AppName = "HSPrint";
  private const string RegistryPath = @"Software\HSPrint";
    
    /// <summary>
    /// Checks if HSPrint is already installed on this machine
    /// </summary>
    public static bool IsInstalled()
    {
     try
        {
      using var key = Registry.LocalMachine.OpenSubKey(RegistryPath);
            return key != null;
        }
        catch
{
            return false;
        }
    }

    /// <summary>
    /// Gets the currently installed version
    /// </summary>
    public static string? GetInstalledVersion()
{
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryPath);
            return key?.GetValue("Version")?.ToString();
        }
      catch
        {
         return null;
        }
    }

    /// <summary>
    /// Gets the installation path
    /// </summary>
    public static string? GetInstallPath()
    {
        try
        {
         using var key = Registry.LocalMachine.OpenSubKey(RegistryPath);
            return key?.GetValue("InstallPath")?.ToString();
}
        catch
        {
       return null;
        }
 }

    /// <summary>
  /// Uninstalls the current version of HSPrint silently
    /// </summary>
    public static async Task<bool> UninstallExisting()
    {
        try
        {
   // Find the product code from the registry
            string? productCode = FindProductCode();
          if (productCode == null)
  {
          Console.WriteLine("No existing installation found to uninstall.");
                return true;
     }

      Console.WriteLine($"Uninstalling existing version...");

        var processInfo = new ProcessStartInfo
            {
    FileName = "msiexec.exe",
 Arguments = $"/x {productCode} /qn /norestart",
     UseShellExecute = false,
            CreateNoWindow = true,
       RedirectStandardOutput = true,
       RedirectStandardError = true
            };

  using var process = Process.Start(processInfo);
     if (process != null)
    {
          await process.WaitForExitAsync();
        
         // Wait a bit for the uninstall to complete
       await Task.Delay(2000);
             
      return process.ExitCode == 0;
   }

          return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uninstalling: {ex.Message}");
    return false;
        }
    }

  /// <summary>
    /// Stops the HSPrint service if running
    /// </summary>
    public static async Task<bool> StopService()
    {
try
        {
            var processInfo = new ProcessStartInfo
          {
                FileName = "sc.exe",
     Arguments = "stop HSPrintService",
                UseShellExecute = false,
       CreateNoWindow = true,
 RedirectStandardOutput = true
   };

       using var process = Process.Start(processInfo);
      if (process != null)
   {
             await process.WaitForExitAsync();
     await Task.Delay(1000); // Wait for service to stop
                return true;
            }

 return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Stops all running instances of HSPrint
    /// </summary>
    public static void StopRunningInstances()
    {
        try
     {
     var processes = Process.GetProcessesByName("HSPrint");
  foreach (var process in processes)
  {
          try
{
     process.Kill();
            process.WaitForExit(5000);
       }
      catch
         {
  // Ignore errors killing individual processes
           }
   }
        }
        catch
      {
      // Ignore errors
        }
    }

    /// <summary>
    /// Clears application cache and temporary files
    /// </summary>
    public static void ClearCache()
    {
        try
   {
  string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppName
            );

            if (Directory.Exists(appDataPath))
            {
       Directory.Delete(appDataPath, true);
        }

            string tempPath = Path.Combine(Path.GetTempPath(), AppName);
         if (Directory.Exists(tempPath))
    {
         Directory.Delete(tempPath, true);
  }
        }
        catch
        {
            // Ignore cache clearing errors
        }
    }

    /// <summary>
 /// Configures Windows to run HSPrint on startup
    /// </summary>
    public static bool ConfigureStartup(bool enable)
    {
        try
        {
   using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", 
                true
      );

         if (key == null) return false;

            if (enable)
            {
    string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
      key.SetValue(AppName, $"\"{exePath}\"");
      }
     else
            {
       key.DeleteValue(AppName, false);
            }

            return true;
        }
      catch
        {
  return false;
        }
    }

    /// <summary>
  /// Finds the MSI product code for HSPrint
    /// </summary>
    private static string? FindProductCode()
    {
        try
        {
         using var key = Registry.LocalMachine.OpenSubKey(
    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
            );

       if (key == null) return null;

      foreach (string subKeyName in key.GetSubKeyNames())
            {
       using var subKey = key.OpenSubKey(subKeyName);
       if (subKey == null) continue;

                var displayName = subKey.GetValue("DisplayName")?.ToString();
         if (displayName != null && displayName.Contains(AppName))
  {
   return subKeyName;
          }
       }

            // Also check 64-bit registry on 64-bit systems
            if (Environment.Is64BitOperatingSystem)
            {
       using var key64 = Registry.LocalMachine.OpenSubKey(
         @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                );

if (key64 == null) return null;

                foreach (string subKeyName in key64.GetSubKeyNames())
 {
             using var subKey = key64.OpenSubKey(subKeyName);
         if (subKey == null) continue;

          var displayName = subKey.GetValue("DisplayName")?.ToString();
  if (displayName != null && displayName.Contains(AppName))
        {
 return subKeyName;
        }
     }
            }

            return null;
    }
        catch
        {
            return null;
        }
    }
}
