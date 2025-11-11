# HSPrint Quick Reference

## Installation Commands

### Install from MSI
```powershell
# Double-click or run:
.\HSPrintSetup-1.0.0.msi

# Silent install
msiexec /i HSPrintSetup-1.0.0.msi /qn /norestart

# Install with PowerShell script
.\install.ps1
.\install.ps1 -Silent
.\install.ps1 -MsiPath "C:\Path\To\HSPrintSetup.msi"
```

### Uninstall
```powershell
# Via Control Panel
# "Add or Remove Programs" ? "HSPrint" ? "Uninstall"

# Via PowerShell
$app = Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\* | Where-Object { $_.DisplayName -like "*HSPrint*" }
msiexec /x $app.PSChildName /qn /norestart
```

## Service Management

### Check Service Status
```powershell
Get-Service HSPrintService
sc query HSPrintService
```

### Start/Stop Service
```powershell
Start-Service HSPrintService
Stop-Service HSPrintService
Restart-Service HSPrintService
```

### Configure Service
```powershell
# Set to automatic startup
sc config HSPrintService start=auto

# Set to manual startup
sc config HSPrintService start=demand

# Disable
sc config HSPrintService start=disabled
```

## Development Commands

### Build
```powershell
# Restore dependencies
dotnet restore

# Build
dotnet build

# Build release
dotnet build -c Release

# Run
dotnet run

# Watch mode (auto-reload)
dotnet watch run
```

### Build Installer
```powershell
# Using script
.\build-installer.ps1 -Version "1.0.0"

# Using batch file
build-installer.bat

# Manual build
dotnet publish HSPrint.csproj -c Release -r win-x64 --self-contained true -o ./artifacts/publish
dotnet build HSPrint.Installer/HSPrint.Installer.wixproj -c Release
```

## Testing Commands

### Test API
```powershell
# Health check
curl http://localhost:50246/health

# Get version
curl http://localhost:50246/health/version

# List printers
curl http://localhost:50246/printer

# Test print (replace printer name)
curl -X POST http://localhost:50246/print/zpl `
     -H "Content-Type: application/json" `
   -d '{"printerName":"Zebra ZP450","zpl":"^XA^FO50,50^ADN,36,20^FDTest^FS^XZ"}'
```

### Test with PowerShell
```powershell
# Health check
Invoke-RestMethod -Uri "http://localhost:50246/health"

# List printers
Invoke-RestMethod -Uri "http://localhost:50246/printer"

# Print ZPL
$body = @{
    printerName = "Zebra ZP450"
    zpl = "^XA^FO50,50^ADN,36,20^FDTest^FS^XZ"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:50246/print/zpl" `
            -Method Post `
  -Body $body `
               -ContentType "application/json"
```

## Git/Release Commands

### Create Release
```bash
# Update version
echo "1.0.0" > version.txt

# Commit
git add .
git commit -m "Release v1.0.0"
git push origin master

# Create and push tag
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0
```

### Check Tags
```bash
# List all tags
git tag

# Show tag info
git show v1.0.0

# Delete local tag
git tag -d v1.0.0

# Delete remote tag
git push origin :refs/tags/v1.0.0
```

## Troubleshooting Commands

### Check Logs
```powershell
# Application logs
Get-Content "C:\Program Files\HSPrint\logs\hsprinteragent-$(Get-Date -Format 'yyyyMMdd').log" -Tail 50

# Windows Event Log
Get-EventLog -LogName Application -Source HSPrintService -Newest 10

# Installation log
Get-Content "$env:TEMP\HSPrint-Install.log" -Tail 50
```

### Check Installation
```powershell
# Check if installed
Test-Path "HKLM:\Software\HSPrint"

# Get install path
(Get-ItemProperty "HKLM:\Software\HSPrint").InstallPath

# Get installed version
(Get-ItemProperty "HKLM:\Software\HSPrint").Version

# Check startup entry
Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "HSPrint"
```

### Check Port
```powershell
# Check if port 50246 is in use
Get-NetTCPConnection -LocalPort 50246

# Find process using port
Get-Process -Id (Get-NetTCPConnection -LocalPort 50246).OwningProcess
```

### Kill Process
```powershell
# Find and stop HSPrint processes
Get-Process -Name "HSPrint" | Stop-Process -Force

# Stop service
Stop-Service HSPrintService -Force
```

### Clear Cache
```powershell
# Clear application cache
Remove-Item "$env:LOCALAPPDATA\HSPrint" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$env:TEMP\HSPrint" -Recurse -Force -ErrorAction SilentlyContinue

# Clear logs
Remove-Item "C:\Program Files\HSPrint\logs\*" -Force -ErrorAction SilentlyContinue
```

### Reinstall Clean
```powershell
# Complete clean reinstall
# 1. Stop everything
Stop-Service HSPrintService -Force -ErrorAction SilentlyContinue
Get-Process -Name "HSPrint" -ErrorAction SilentlyContinue | Stop-Process -Force

# 2. Uninstall
$app = Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\* | Where-Object { $_.DisplayName -like "*HSPrint*" }
if ($app) { msiexec /x $app.PSChildName /qn /norestart }

# 3. Wait
Start-Sleep -Seconds 5

# 4. Clean up
Remove-Item "C:\Program Files\HSPrint" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$env:LOCALAPPDATA\HSPrint" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$env:TEMP\HSPrint" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "HKLM:\Software\HSPrint" -Recurse -Force -ErrorAction SilentlyContinue

# 5. Reinstall
.\install.ps1 -MsiPath "HSPrintSetup-1.0.0.msi"
```

## Network/Firewall Commands

### Check Firewall Rules
```powershell
# List rules for HSPrint
Get-NetFirewallRule | Where-Object { $_.DisplayName -like "*HSPrint*" }

# Add firewall rule (if needed for remote access)
New-NetFirewallRule -DisplayName "HSPrint API" `
    -Direction Inbound `
       -Protocol TCP `
         -LocalPort 50246 `
  -Action Allow
```

### Test Network Connectivity
```powershell
# Test from another machine
Test-NetConnection -ComputerName <server-ip> -Port 50246

# Listen on all interfaces (NOT RECOMMENDED for production)
# Edit appsettings.json: "Urls": "http://*:50246"
```

## Configuration

### View Configuration
```powershell
# View appsettings.json
Get-Content "C:\Program Files\HSPrint\appsettings.json" | ConvertFrom-Json | ConvertTo-Json -Depth 10

# Edit configuration
notepad "C:\Program Files\HSPrint\appsettings.json"

# Restart after config change
Restart-Service HSPrintService
```

### Common Configuration Changes
```json
{
  "Urls": "http://localhost:50246",
  "UpdateCheckUrl": "https://hssoftware.nl/api/agent/latest",
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  }
}
```

## Printer Commands

### List Printers (Windows)
```powershell
# List all printers
Get-Printer | Select-Object Name, DriverName, PortName

# List only specific printers
Get-Printer | Where-Object { $_.Name -like "*Zebra*" }

# Get printer details
Get-Printer -Name "Zebra ZP450" | Format-List *
```

### Test Print (Windows)
```powershell
# Test page
$printer = Get-Printer -Name "Zebra ZP450"
Invoke-Command { rundll32 printui.dll,PrintUIEntry /k /n $printer.Name }
```

## Useful Paths

```powershell
# Installation directory
C:\Program Files\HSPrint\

# Logs directory
C:\Program Files\HSPrint\logs\

# Local app data
$env:LOCALAPPDATA\HSPrint

# Temp directory
$env:TEMP\HSPrint

# Registry
HKLM:\Software\HSPrint
HKCU:\Software\Microsoft\Windows\CurrentVersion\Run\HSPrint
```

## API Endpoints Quick Reference

```
GET  /  - API info
GET  /health   - Health check
GET  /health/version   - Get version
GET  /health/update      - Check for updates
GET  /printer    - List printers
POST /print/zpl          - Print ZPL to local printer
POST /print/zpl/tcp             - Print ZPL via TCP/IP
POST /print/image               - Print PNG image
POST /print/pdf    - Print PDF document
```

## Emergency Recovery

### If everything fails:
```powershell
# Nuclear option - complete removal and reinstall
# WARNING: This removes everything!

# 1. Stop all HSPrint processes
Get-Process -Name "*HSPrint*" -ErrorAction SilentlyContinue | Stop-Process -Force
Stop-Service HSPrintService -Force -ErrorAction SilentlyContinue
sc delete HSPrintService

# 2. Remove all files and registry
Remove-Item "C:\Program Files\HSPrint" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$env:LOCALAPPDATA\HSPrint" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$env:TEMP\HSPrint*" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "HKLM:\Software\HSPrint" -Recurse -Force -ErrorAction SilentlyContinue
Remove-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "HSPrint" -ErrorAction SilentlyContinue

# 3. Reboot
Restart-Computer

# 4. After reboot, reinstall
.\install.ps1 -MsiPath "HSPrintSetup-1.0.0.msi"
```

---

**For more detailed information, see:**
- [README.md](README.md) - Full documentation
- [INSTALLER.md](INSTALLER.md) - Installer documentation
- [FIRST-RELEASE.md](FIRST-RELEASE.md) - Release guide
