using System.Diagnostics;
using System.ServiceProcess;
using System.Windows.Forms;
using System.Drawing;

namespace HSPrint.ConfigTool;

public partial class ConfigForm : Form
{
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _statusTimer;
    private const string ServiceName = "HSPrintService";
    private const string GitHubReleasesUrl = "https://github.com/HiddeS03/HSPrint/releases";
    private const string LogDirectory = @"C:\Program Files\HSPrint\logs";

    public ConfigForm()
    {
        InitializeComponent();

        // Setup system tray icon
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "HSPrint Configuration",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };
        _notifyIcon.DoubleClick += (s, e) => ShowForm();

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
        Activate();
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
                txtLogs.Text = "Log directory not found.";
                return;
            }

            var logFiles = Directory.GetFiles(LogDirectory, "*.log")
                                   .OrderByDescending(f => File.GetLastWriteTime(f))
                                   .Take(1)
                                   .ToList();

            if (logFiles.Any())
            {
                var latestLog = logFiles.First();
                var lines = File.ReadAllLines(latestLog);
                txtLogs.Text = string.Join(Environment.NewLine, lines.TakeLast(500));
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
            if (service != null && service.Status == ServiceControllerStatus.Running)
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                UpdateServiceStatus();
                MessageBox.Show("Service stopped successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error stopping service: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

}
