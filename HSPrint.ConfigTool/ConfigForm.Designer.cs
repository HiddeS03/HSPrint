namespace HSPrint.ConfigTool;

partial class ConfigForm
{
    private System.ComponentModel.IContainer components = null;
    private Label lblTitle;
    private GroupBox grpServiceControl;
    private Label lblServiceStatusLabel;
    private Label lblServiceStatus;
    private Button btnStartService;
    private Button btnStopService;
    private GroupBox grpSettings;
    private CheckBox chkStartWithWindows;
    private GroupBox grpLogs;
    private TextBox txtLogs;
    private Button btnRefreshLogs;
    private Button btnOpenGitHub;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon?.Dispose();
            _statusTimer?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.lblTitle = new Label();
        this.grpServiceControl = new GroupBox();
        this.lblServiceStatusLabel = new Label();
        this.lblServiceStatus = new Label();
        this.btnStartService = new Button();
        this.btnStopService = new Button();
        this.grpSettings = new GroupBox();
        this.chkStartWithWindows = new CheckBox();
        this.btnOpenGitHub = new Button();
        this.grpLogs = new GroupBox();
        this.txtLogs = new TextBox();
        this.btnRefreshLogs = new Button();

        this.SuspendLayout();

        // lblTitle
        this.lblTitle.AutoSize = true;
        this.lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        this.lblTitle.Location = new Point(20, 20);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.Size = new Size(300, 30);
        this.lblTitle.TabIndex = 0;
        this.lblTitle.Text = "HSPrint Configuration";

        // grpServiceControl
        this.grpServiceControl.Controls.Add(this.lblServiceStatusLabel);
        this.grpServiceControl.Controls.Add(this.lblServiceStatus);
        this.grpServiceControl.Controls.Add(this.btnStartService);
        this.grpServiceControl.Controls.Add(this.btnStopService);
        this.grpServiceControl.Location = new Point(20, 60);
        this.grpServiceControl.Name = "grpServiceControl";
        this.grpServiceControl.Size = new Size(760, 100);
        this.grpServiceControl.TabIndex = 1;
        this.grpServiceControl.TabStop = false;
        this.grpServiceControl.Text = "Service Control";

        // lblServiceStatusLabel
        this.lblServiceStatusLabel.AutoSize = true;
        this.lblServiceStatusLabel.Location = new Point(20, 35);
        this.lblServiceStatusLabel.Name = "lblServiceStatusLabel";
        this.lblServiceStatusLabel.Size = new Size(90, 15);
        this.lblServiceStatusLabel.TabIndex = 0;
        this.lblServiceStatusLabel.Text = "Service Status:";

        // lblServiceStatus
        this.lblServiceStatus.AutoSize = true;
        this.lblServiceStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        this.lblServiceStatus.Location = new Point(120, 35);
        this.lblServiceStatus.Name = "lblServiceStatus";
        this.lblServiceStatus.Size = new Size(60, 15);
        this.lblServiceStatus.TabIndex = 1;
        this.lblServiceStatus.Text = "Checking...";

        // btnStartService
        this.btnStartService.Location = new Point(20, 60);
        this.btnStartService.Name = "btnStartService";
        this.btnStartService.Size = new Size(100, 30);
        this.btnStartService.TabIndex = 2;
        this.btnStartService.Text = "Start Service";
        this.btnStartService.UseVisualStyleBackColor = true;
        this.btnStartService.Click += new EventHandler(this.btnStartService_Click);

        // btnStopService
        this.btnStopService.Location = new Point(130, 60);
        this.btnStopService.Name = "btnStopService";
        this.btnStopService.Size = new Size(100, 30);
        this.btnStopService.TabIndex = 3;
        this.btnStopService.Text = "Stop Service";
        this.btnStopService.UseVisualStyleBackColor = true;
        this.btnStopService.Click += new EventHandler(this.btnStopService_Click);

        // grpSettings
        this.grpSettings.Controls.Add(this.chkStartWithWindows);
        this.grpSettings.Controls.Add(this.btnOpenGitHub);
        this.grpSettings.Location = new Point(20, 170);
        this.grpSettings.Name = "grpSettings";
        this.grpSettings.Size = new Size(760, 80);
        this.grpSettings.TabIndex = 2;
        this.grpSettings.TabStop = false;
        this.grpSettings.Text = "Settings";

        // chkStartWithWindows
        this.chkStartWithWindows.AutoSize = true;
        this.chkStartWithWindows.Location = new Point(20, 30);
        this.chkStartWithWindows.Name = "chkStartWithWindows";
        this.chkStartWithWindows.Size = new Size(250, 19);
        this.chkStartWithWindows.TabIndex = 0;
        this.chkStartWithWindows.Text = "Start configuration tool with Windows";
        this.chkStartWithWindows.UseVisualStyleBackColor = true;
        this.chkStartWithWindows.CheckedChanged += new EventHandler(this.chkStartWithWindows_CheckedChanged);

        // btnOpenGitHub
        this.btnOpenGitHub.Location = new Point(20, 50);
        this.btnOpenGitHub.Name = "btnOpenGitHub";
        this.btnOpenGitHub.Size = new Size(150, 25);
        this.btnOpenGitHub.TabIndex = 1;
        this.btnOpenGitHub.Text = "Check for Updates";
        this.btnOpenGitHub.UseVisualStyleBackColor = true;
        this.btnOpenGitHub.Click += new EventHandler(this.btnOpenGitHub_Click);

        // grpLogs
        this.grpLogs.Controls.Add(this.txtLogs);
        this.grpLogs.Controls.Add(this.btnRefreshLogs);
        this.grpLogs.Location = new Point(20, 260);
        this.grpLogs.Name = "grpLogs";
        this.grpLogs.Size = new Size(760, 300);
        this.grpLogs.TabIndex = 3;
        this.grpLogs.TabStop = false;
        this.grpLogs.Text = "Service Logs";

        // txtLogs
        this.txtLogs.Font = new Font("Consolas", 9F);
        this.txtLogs.Location = new Point(20, 50);
        this.txtLogs.Multiline = true;
        this.txtLogs.Name = "txtLogs";
        this.txtLogs.ReadOnly = true;
        this.txtLogs.ScrollBars = ScrollBars.Vertical;
        this.txtLogs.Size = new Size(720, 230);
        this.txtLogs.TabIndex = 1;

        // btnRefreshLogs
        this.btnRefreshLogs.Location = new Point(20, 20);
        this.btnRefreshLogs.Name = "btnRefreshLogs";
        this.btnRefreshLogs.Size = new Size(100, 25);
        this.btnRefreshLogs.TabIndex = 0;
        this.btnRefreshLogs.Text = "Refresh Logs";
        this.btnRefreshLogs.UseVisualStyleBackColor = true;
        this.btnRefreshLogs.Click += new EventHandler(this.btnRefreshLogs_Click);

        // ConfigForm
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(800, 580);
        this.Controls.Add(this.lblTitle);
        this.Controls.Add(this.grpServiceControl);
        this.Controls.Add(this.grpSettings);
        this.Controls.Add(this.grpLogs);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.Name = "ConfigForm";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "HSPrint Configuration";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
