namespace HSPrint.ConfigTool;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Prevent multiple instances
        using var mutex = new System.Threading.Mutex(true, "HSPrintConfigTool", out bool createdNew);
        
        if (!createdNew)
        {
            // Another instance is already running
            MessageBox.Show("HSPrint Configuration Tool is already running. Check the system tray.", 
                "Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new ConfigForm());
    }    
}