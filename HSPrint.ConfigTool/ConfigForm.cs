using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Windows.Forms;

namespace HSPrint.ConfigTool;

public partial class ConfigForm : Form
{
    // DWM API for title bar coloring
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CAPTION_COLOR = 35;

    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _statusTimer;
    private const string ServiceName = "HSPrintService";
    private const string GitHubReleasesUrl = "https://github.com/HiddeS03/HSPrint/releases";
    private static readonly string LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "HSPrint", "logs");

    public ConfigForm()
    {
        InitializeComponent();

        // Apply custom title bar color #287583 (RGB: 40, 117, 131)
        ApplyTitleBarColor();

        // Setup system tray icon
        _notifyIcon = new NotifyIcon
        {
            Icon = LoadApplicationIcon(),
            Text = "HSPrint Configuration",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };
        _notifyIcon.DoubleClick += (s, e) => ShowForm();

        // Set form icon
        this.Icon = _notifyIcon.Icon;

        // Position window bottom right near system tray
        PositionWindowBottomRight();

        // Setup status timer to refresh service status
        _statusTimer = new System.Windows.Forms.Timer
        {
            Interval = 2000 // Check every 2 seconds
        };
        _statusTimer.Tick += (s, e) => UpdateServiceStatus();
        _statusTimer.Start();

        // Initial status update
        UpdateServiceStatus();
        UpdateStartupStatus();
        LoadLogs();
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show Configuration", null, (s, e) => ShowForm());
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (s, e) => ExitApplication());
        return menu;
    }

    private void ShowForm()
    {
        Show();
        WindowState = FormWindowState.Normal;
        PositionWindowBottomRight();
        Activate();
    }

    private void ApplyTitleBarColor()
    {
        if (Environment.OSVersion.Version.Build >= 22000) // Windows 11
        {
            // Color #287583 (RGB: 40, 117, 131) in BGR format: 0x00837528
            int titleBarColor = 0x00837528;
            DwmSetWindowAttribute(this.Handle, DWMWA_CAPTION_COLOR, ref titleBarColor, sizeof(int));
        }
    }

    private void PositionWindowBottomRight()
    {
        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.PrimaryScreen!.Bounds;
        this.StartPosition = FormStartPosition.Manual;
        this.Location = new Point(
            workingArea.Right - this.Width - 20,
            workingArea.Bottom - this.Height - 20
        );
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        Application.Exit();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    private void UpdateServiceStatus()
    {
        try
        {
            var service = GetService();
            if (service != null)
            {
                service.Refresh();
                bool isRunning = service.Status == ServiceControllerStatus.Running;

                lblServiceStatus.Text = isRunning ? "Running" : "Stopped";
                lblServiceStatus.ForeColor = isRunning ? Color.Green : Color.Red;
                btnStartService.Enabled = !isRunning;
                btnStopService.Enabled = isRunning;

                // Update tray icon tooltip
                _notifyIcon.Text = $"HSPrint - {(isRunning ? "Running" : "Stopped")}";
            }
            else
            {
                lblServiceStatus.Text = "Not Installed";
                lblServiceStatus.ForeColor = Color.Orange;
                btnStartService.Enabled = false;
                btnStopService.Enabled = false;
                _notifyIcon.Text = "HSPrint - Not Installed";
            }
        }
        catch (Exception ex)
        {
            lblServiceStatus.Text = "Error: " + ex.Message;
            lblServiceStatus.ForeColor = Color.Red;
        }
    }

    private void UpdateStartupStatus()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run");
            var value = key?.GetValue("HSPrintConfig");
            chkStartWithWindows.Checked = value != null;
        }
        catch
        {
            chkStartWithWindows.Checked = false;
        }
    }

    private void LoadLogs()
    {
        try
        {
            if (!Directory.Exists(LogDirectory))
            {
                txtLogs.Text = $"Log directory not found: {LogDirectory}\n\nLogs will be created here when the service runs.";
                return;
            }

            var logFiles = Directory.GetFiles(LogDirectory, "*.log")
                                   .OrderByDescending(f => File.GetLastWriteTime(f))
                                   .Take(1)
                                   .ToList();

            if (logFiles.Any())
            {
                var latestLog = logFiles.First();

                // Open file with FileShare.ReadWrite to allow reading while service is writing
                using var fileStream = new FileStream(latestLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream);

                var allLines = new List<string>();
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    allLines.Add(line);
                }

                txtLogs.Text = string.Join(Environment.NewLine, allLines.TakeLast(500));
                txtLogs.SelectionStart = txtLogs.Text.Length;
                txtLogs.ScrollToCaret();
            }
            else
            {
                txtLogs.Text = "No log files found.";
            }
        }
        catch (Exception ex)
        {
            txtLogs.Text = $"Error loading logs: {ex.Message}";
        }
    }

    private ServiceController? GetService()
    {
        try
        {
            return ServiceController.GetServices()
                .FirstOrDefault(s => s.ServiceName == ServiceName);
        }
        catch
        {
            return null;
        }
    }

    private void btnStartService_Click(object sender, EventArgs e)
    {
        try
        {
            var service = GetService();
            if (service != null && service.Status != ServiceControllerStatus.Running)
            {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
                UpdateServiceStatus();
                MessageBox.Show("Service started successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error starting service: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnStopService_Click(object sender, EventArgs e)
    {
        try
        {
            var service = GetService();
            if (service == null)
            {
                MessageBox.Show("Service not found. Please check if HSPrint is installed correctly.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (service.Status == ServiceControllerStatus.Running)
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                UpdateServiceStatus();
                MessageBox.Show("Service stopped successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Service is not running. Current status: {service.Status}", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (System.InvalidOperationException ex)
        {
            MessageBox.Show($"Cannot access service. Please run as administrator.\n\nDetails: {ex.Message}", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error stopping service: {ex.Message}\n\nPlease ensure you have administrative privileges.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void chkStartWithWindows_CheckedChanged(object sender, EventArgs e)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);

            if (key != null)
            {
                if (chkStartWithWindows.Checked)
                {
                    var exePath = Application.ExecutablePath;
                    key.SetValue("HSPrintConfig", $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue("HSPrintConfig", false);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating startup settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            chkStartWithWindows.Checked = !chkStartWithWindows.Checked;
        }
    }

    private void btnRefreshLogs_Click(object sender, EventArgs e)
    {
        LoadLogs();
    }

    private void btnOpenGitHub_Click(object sender, EventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GitHubReleasesUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening GitHub: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Icon LoadApplicationIcon()
    {
        try
        {
            // Try to load icon from file (when running from installation directory)
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }

            // Try to load from assets directory (development)
            iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "assets", "logo.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch
        {
            // Ignore errors and fall back to default
        }

        // Fallback to system icon
        return SystemIcons.Application;
    }
}
