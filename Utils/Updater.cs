using System.Diagnostics;
using System.Text.Json;

namespace HSPrint.Utils;

public class Updater
{
    private readonly ILogger<Updater> _logger;
    private readonly string _currentVersion;
    private readonly string _updateCheckUrl;

    public Updater(ILogger<Updater> logger, string currentVersion, string updateCheckUrl = "https://hssoftware.nl/api/agent/latest")
    {
  _logger = logger;
      _currentVersion = currentVersion;
        _updateCheckUrl = updateCheckUrl;
    }

    public async Task<UpdateInfo?> CheckForUpdates()
    {
        try
        {
            _logger.LogInformation("Checking for updates. Current version: {Version}", _currentVersion);
       
          using var httpClient = new HttpClient();
    httpClient.Timeout = TimeSpan.FromSeconds(10);
 
    var response = await httpClient.GetStringAsync(_updateCheckUrl);
       var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(response);
   
    if (updateInfo == null)
      {
  _logger.LogWarning("Failed to parse update information");
             return null;
            }
 
            if (IsNewerVersion(updateInfo.Version, _currentVersion))
   {
          _logger.LogInformation("New version available: {NewVersion}", updateInfo.Version);
    return updateInfo;
       }
            else
            {
       _logger.LogInformation("Application is up to date");
         return null;
      }
        }
 catch (Exception ex)
        {
          _logger.LogError(ex, "Error checking for updates");
     return null;
        }
    }

    public async Task<bool> DownloadAndInstallUpdate(UpdateInfo updateInfo)
 {
    try
        {
    _logger.LogInformation("Downloading update from {Url}", updateInfo.DownloadUrl);
    
 string tempPath = Path.Combine(Path.GetTempPath(), $"HSPrint_Update_{updateInfo.Version}.msi");

            using var httpClient = new HttpClient();
          httpClient.Timeout = TimeSpan.FromMinutes(5);
    
       var fileBytes = await httpClient.GetByteArrayAsync(updateInfo.DownloadUrl);
            await File.WriteAllBytesAsync(tempPath, fileBytes);
        
        _logger.LogInformation("Downloaded update to {Path}", tempPath);
  _logger.LogInformation("Starting installation of version {Version}", updateInfo.Version);
            
            // Start MSI installation
          var processInfo = new ProcessStartInfo
            {
           FileName = "msiexec.exe",
          Arguments = $"/i \"{tempPath}\" /quiet /norestart",
UseShellExecute = false,
                CreateNoWindow = true
};
         
            Process.Start(processInfo);
         
            _logger.LogInformation("Update installation started. Application will close.");
            return true;
        }
        catch (Exception ex)
{
_logger.LogError(ex, "Error downloading and installing update");
   return false;
    }
    }

    private bool IsNewerVersion(string remoteVersion, string currentVersion)
    {
        try
   {
            var remote = new Version(remoteVersion);
 var current = new Version(currentVersion);
    return remote > current;
     }
        catch
        {
   return false;
        }
    }
}

public class UpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string ReleaseNotes { get; set; } = string.Empty;
}
