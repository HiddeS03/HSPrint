# HSPrint - Local Printer Agent

![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)

HSPrint is a local Windows service/console application that provides a REST API for printing to local printers from web applications. It supports ZPL (Zebra), image (PNG), and PDF printing.

## 🎯 Features

- **Multiple Print Formats**: ZPL (Zebra), PNG images, and PDF documents
- **Local & Network Printing**: Print to USB/local printers or network printers via TCP/IP
- **REST API**: Simple HTTP API for easy integration
- **Windows Service**: Runs as a service with automatic startup
- **Auto-Update**: Automatic version checking and updates
- **MSI Installer**: Professional Windows installer with auto-cleanup
- **Logging**: Comprehensive logging with Serilog
- **CORS Support**: Configured for web application access
- **Swagger Documentation**: Interactive API documentation

## 📋 Prerequisites

- Windows 10/11 or Windows Server 2016+
- Administrator rights (for installation and printer access)

## 🚀 Quick Start

### Installation (Production)

**Option 1: MSI Installer (Recommended)**

1. **Download the latest release** from [GitHub Releases](https://github.com/HiddeS03/HSPrint/releases)
   - Download `HSPrintSetup-x.x.x.msi`

2. **Run the installer**
   - Double-click the MSI file
   - Follow the installation wizard
   - HSPrint will automatically start and configure itself to run on Windows startup

3. **Verify installation**
   ```powershell
   curl http://localhost:50246/health
   ```

**Option 2: Automated PowerShell Installation**

1. **Download files**
   - `HSPrintSetup-x.x.x.msi`
 - `install.ps1`

2. **Run as Administrator**
   ```powershell
   # Interactive installation
   .\install.ps1

   # Silent installation
   .\install.ps1 -Silent
   ```

The installer will automatically:
- ✅ Stop running HSPrint instances
- ✅ Uninstall previous versions
- ✅ Clear cache for clean installation
- ✅ Install new version
- ✅ Configure Windows startup
- ✅ Start the service

**Option 3: Portable ZIP**

For testing or portable deployment:
1. Download `HSPrint-x.x.x.zip`
2. Extract to your preferred location
3. Run `HSPrint.exe`

> ⚠️ **Note**: ZIP version does not install as a service or configure automatic startup

### Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/HiddeS03/HSPrint.git
   cd HSPrint
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run in development mode**
   ```bash
   dotnet watch run
   ```

4. **Open Swagger UI**
   Navigate to: `http://localhost:50246`

## 📦 Building the Installer

See [INSTALLER.md](INSTALLER.md) for detailed instructions on building and customizing the installer.

**Quick build:**

```powershell
# Using the build script
.\build-installer.ps1 -Version "1.0.0"

# Or double-click
build-installer.bat
```

**Prerequisites for building:**
- .NET 8 SDK
- WiX Toolset 4: `dotnet tool install --global wix --version 4.0.5`

## 🔄 Updating

The installer handles updates automatically:

1. Run the new installer
2. It detects the existing installation
3. Automatically uninstalls the old version
4. Installs the new version
5. Preserves your configuration

**Or use the PowerShell script:**
```powershell
.\install.ps1 -MsiPath "HSPrintSetup-1.1.0.msi"
```

## 🗑️ Uninstalling

**Via Windows Settings:**
1. Open "Add or Remove Programs"
2. Find "HSPrint"
3. Click "Uninstall"

**Via PowerShell:**
```powershell
# Find and uninstall
$app = Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\* | 
        Where-Object { $_.DisplayName -like "*HSPrint*" }
msiexec /x $app.PSChildName /qn
```

## 📡 API Endpoints

### Health & Info

#### `GET /health`
Check if the service is running
```json
{
  "status": "ok",
  "pid": 12345,
  "version": "1.0.0",
  "timestamp": "2024-01-20T10:30:00Z"
}
```

#### `GET /health/version`
Get current version
```json
{
  "version": "1.0.0",
  "timestamp": "2024-01-20T10:30:00Z"
}
```

#### `GET /health/update`
Check for available updates
```json
{
  "updateAvailable": true,
  "currentVersion": "1.0.0",
  "newVersion": "1.1.0",
  "downloadUrl": "https://...",
  "releaseNotes": "..."
}
```

### Printers

#### `GET /printer`
List all installed printers
```json
[
  "Microsoft Print to PDF",
  "Zebra ZP450",
  "EPSON L3250"
]
```

### Printing

#### `POST /print/zpl`
Print ZPL to a local printer
```json
{
  "printerName": "Zebra ZP450",
  "zpl": "^XA^FO50,50^ADN,36,20^FDHello World^FS^XZ"
}
```

#### `POST /print/zpl/tcp`
Print ZPL via TCP/IP (network printer)
```json
{
  "ip": "192.168.1.100",
  "port": 9100,
  "zpl": "^XA^FO50,50^ADN,36,20^FDHello World^FS^XZ"
}
```

#### `POST /print/image`
Print PNG image
```json
{
  "printerName": "EPSON L3250",
  "base64Png": "iVBORw0KGgoAAAANSUhEUgA..."
}
```

#### `POST /print/pdf`
Print PDF document
```json
{
  "printerName": "Microsoft Print to PDF",
  "base64Pdf": "JVBERi0xLjQKJeLjz9MKMSAw..."
}
```

## 🔧 Configuration

Edit `appsettings.json` to customize:

```json
{
  "Version": "1.0.0",
  "Urls": "http://localhost:50246",
  "UpdateCheckUrl": "https://hssoftware.nl/api/agent/latest",
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3001",
      "https://hssoftware.nl"
    ]
  }
}
```

## 📁 Project Structure

```
HSPrint/
├── Controllers/
│   ├── HealthController.cs    # Health check & version endpoints
│   ├── PrinterController.cs   # Printer enumeration
│   └── PrintController.cs     # Print job endpoints
├── Services/
│ ├── IPrinterService.cs   # Printer service interface
│   ├── PrinterService.cs      # Printer service implementation
│   ├── IPrintService.cs       # Print service interface
│   └── PrintService.cs     # Print service implementation
├── Utils/
│ ├── RawPrinterHelper.cs    # Windows printer P/Invoke
│   └── Updater.cs      # Auto-update functionality
├── Program.cs          # Application entry point
├── appsettings.json        # Configuration
└── version.txt            # Current version
```

## 🛠️ Development

### Watch Mode
```bash
dotnet watch run
```
The application will automatically restart on file changes.

### Build
```bash
dotnet build --configuration Release
```

### Publish (Self-Contained)
```bash
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

### Run Tests
```bash
dotnet test
```

## 🔄 Auto-Update System

The application can automatically check for updates and install them:

1. **Version Check**: Calls `https://hssoftware.nl/api/agent/latest` for latest version info
2. **Comparison**: Compares with local `version.txt`
3. **Download**: Downloads MSI/EXE if newer version available
4. **Install**: Runs `msiexec /i` to install update
5. **Restart**: Application restarts with new version

### Expected Update API Response
```json
{
  "version": "1.1.0",
  "downloadUrl": "https://hssoftware.nl/downloads/HSPrint-1.1.0.msi",
  "releaseNotes": "Bug fixes and improvements"
}
```

## 📦 Deployment

### GitHub Actions

The project includes a GitHub Actions workflow that:
- Builds the application on every push to `main`
- Publishes as self-contained Windows executable
- Uploads to Supabase Storage
- Creates GitHub releases for version tags

### Required Secrets
- `SUPABASE_URL`: Your Supabase project URL
- `SUPABASE_SERVICE_ROLE_KEY`: Service role key for storage access

### Triggering a Release
```bash
git tag v1.0.0
git push origin v1.0.0
```

## 🐛 Troubleshooting

### Application won't start
- Check if port 50246 is available
- Ensure .NET 8 runtime is installed
- Check logs in `logs/hsprinteragent-*.log`

### Printer not found
- Verify printer is installed and visible in Windows
- Check printer name matches exactly (case-sensitive)
- Ensure printer is not offline

### ZPL not printing
- Verify printer supports ZPL (Zebra printers)
- Check ZPL syntax
- Try TCP/IP printing if local printing fails

### CORS errors
- Add your domain to `appsettings.json` under `Cors:AllowedOrigins`
- Ensure the application is running on localhost only

### Logs location
- Default: `logs/hsprinteragent-YYYYMMDD.log`
- Logs rotate daily
- Kept for 30 days

## 🔐 Security

- **Localhost Only**: Application binds only to `localhost` (127.0.0.1)
- **No Authentication**: Currently no auth - relies on localhost security
- **CORS**: Whitelist only trusted origins
- **Future**: Add API key authentication for production use

## 🚧 Roadmap

- [ ] Windows Service support
- [ ] Linux support (CUPS integration)
- [ ] API key authentication
- [ ] Print job queue and history
- [ ] Printer status monitoring
- [ ] Email notifications for print errors
- [ ] Docker support
- [ ] macOS support

## 💡 Copilot Instructions

Useful prompts for GitHub Copilot when working with this project:

### Add Features
- "Add endpoint to print PDF file"
- "Add authentication middleware with API keys"
- "Add print job queue with retry logic"
- "Add printer status monitoring endpoint"
- "Implement rate limiting for API endpoints"

### Convert & Extend
- "Convert project to Windows Service"
- "Add Linux support using CUPS"
- "Create Docker container for this application"
- "Add database logging for print history"
- "Implement webhook notifications for print events"

### Installation & Deployment
- "Make WiX installer script for Windows"
- "Create systemd service file for Linux"
- "Add Chocolatey package manifest"
- "Create MSI installer with WiX Toolset"

### Testing
- "Add unit tests for PrintService"
- "Create integration tests for print endpoints"
- "Add mock printer for testing"

### Documentation
- "Generate OpenAPI/Swagger documentation"
- "Create Postman collection from endpoints"
- "Write troubleshooting guide for common issues"

## 📞 Contact & Support

- **Website**: https://hssoftware.nl
- **Email**: support@hssoftware.nl
- **Issues**: https://github.com/yourusername/HSPrint/issues

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- Built with .NET 8
- Uses Serilog for logging
- Swagger/OpenAPI for documentation
- System.Drawing.Common for image printing

---

**Made with ❤️ by HS Software**
