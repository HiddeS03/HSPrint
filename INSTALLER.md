# HSPrint Installer Guide

This document explains how to build, install, and distribute HSPrint using the installer system.

## Overview

HSPrint uses a Windows Installer (MSI) based installation system that:
- Automatically detects and removes existing installations
- Clears cache to ensure clean installation
- Configures Windows startup
- Installs as a Windows Service
- Provides automatic updates

## Building the Installer

### Prerequisites

1. **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **WiX Toolset 4** - Install via:
   ```powershell
   dotnet tool install --global wix --version 4.0.5
   ```

### Build Steps

#### Option 1: Using the Build Script (Recommended)

```powershell
.\build-installer.ps1 -Version "1.0.0"
```

This script will:
- Restore dependencies
- Build the application
- Publish as a self-contained Windows executable
- Create an MSI installer
- Create a ZIP archive
- Output everything to the `artifacts` folder

#### Option 2: Manual Build

```powershell
# Restore and build
dotnet restore
dotnet build -c Release

# Publish application
dotnet publish HSPrint.csproj -c Release -r win-x64 --self-contained true -o ./artifacts/publish

# Build MSI installer
dotnet build HSPrint.Installer/HSPrint.Installer.wixproj -c Release
```

## Installation Methods

### Method 1: MSI Installer (Recommended)

Simply double-click `HSPrintSetup-x.x.x.msi` and follow the installation wizard.

**Features:**
- Automatic uninstall of previous versions
- Windows Service installation
- Startup configuration
- Add/Remove Programs integration
- Clean uninstall

### Method 2: PowerShell Installation Script

For automated or silent installations:

```powershell
# Run as Administrator
.\install.ps1

# Silent installation
.\install.ps1 -Silent

# Specify MSI path
.\install.ps1 -MsiPath "C:\Path\To\HSPrintSetup.msi"
```

The script will:
1. Check for administrator privileges
2. Stop any running HSPrint instances
3. Uninstall existing versions
4. Clear application cache
5. Install the new version
6. Configure startup

### Method 3: Portable ZIP

Extract `HSPrint-x.x.x.zip` and run `HSPrint.exe` directly. This method:
- Does not require installation
- Does not install as a service
- Does not configure automatic startup
- Useful for testing or portable deployment

## Installation Details

### Installation Location

Default: `C:\Program Files\HSPrint\`

### Files Installed

- `HSPrint.exe` - Main application
- `*.dll` - Required libraries
- `appsettings.json` - Configuration
- `version.txt` - Version information
- `logs\` - Log directory

### Registry Keys

- `HKLM\Software\HSPrint` - Installation information
- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\HSPrint` - Startup configuration

### Service Details

- **Name:** HSPrintService
- **Display Name:** HSPrint Printer Agent
- **Description:** Local printer agent for HS Software - Handles ZPL, Image, and PDF printing
- **Startup Type:** Automatic
- **Account:** LocalSystem

## Upgrading

The installer automatically handles upgrades:

1. Detects existing installation
2. Stops the service and running processes
3. Uninstalls the old version
4. Installs the new version
5. Starts the service

**No manual uninstallation required!**

## Uninstallation

### Via Control Panel

1. Open "Add or Remove Programs"
2. Find "HSPrint"
3. Click "Uninstall"

### Via PowerShell

```powershell
# Find the product code
$app = Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\* | 
        Where-Object { $_.DisplayName -like "*HSPrint*" }

# Uninstall
msiexec /x $app.PSChildName /qn
```

### Manual Cleanup

If needed, manually remove:

```powershell
# Stop and remove service
sc stop HSPrintService
sc delete HSPrintService

# Remove files
Remove-Item "C:\Program Files\HSPrint" -Recurse -Force

# Remove registry keys
Remove-Item "HKLM:\Software\HSPrint" -Recurse -Force
Remove-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "HSPrint"

# Remove cache
Remove-Item "$env:LOCALAPPDATA\HSPrint" -Recurse -Force
Remove-Item "$env:TEMP\HSPrint" -Recurse -Force
```

## Creating a GitHub Release

### Automatic (via GitHub Actions)

1. Update `version.txt` with the new version number
2. Commit and push changes
3. Create and push a version tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The GitHub Actions workflow will:
- Build the application
- Create the MSI installer
- Create a ZIP archive
- Upload to Supabase Storage
- Create a GitHub Release with both files

### Manual Release

1. Build the installer:
   ```powershell
   .\build-installer.ps1 -Version "1.0.0"
   ```

2. Create a new release on GitHub:
   - Go to Releases ? Draft a new release
   - Create a tag: `v1.0.0`
   - Upload files from `artifacts/`:
     - `HSPrintSetup-1.0.0.msi`
     - `HSPrint-1.0.0.zip`
     - `install.ps1`

3. Write release notes and publish

## Troubleshooting

### Build Issues

**WiX not found:**
```powershell
dotnet tool install --global wix --version 4.0.5
```

**Missing dependencies:**
```powershell
dotnet restore
```

### Installation Issues

**Installation fails:**
- Check the log: `%TEMP%\HSPrint-Install.log`
- Run as Administrator
- Ensure Windows Installer service is running

**Service won't start:**
```powershell
# Check service status
sc query HSPrintService

# View service logs
Get-EventLog -LogName Application -Source HSPrintService -Newest 10
```

**Port already in use:**
- Check if another instance is running
- Edit `appsettings.json` to change the port

### Startup Issues

**HSPrint doesn't start on Windows startup:**
```powershell
# Verify registry entry
Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "HSPrint"

# Re-add if missing
Set-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "HSPrint" -Value '"C:\Program Files\HSPrint\HSPrint.exe"'
```

## Development

### Testing the Installer

1. Build the installer
2. Test fresh installation on a clean VM
3. Test upgrade from previous version
4. Test uninstallation
5. Verify all features work

### Modifying the Installer

Edit `HSPrint.Installer/Product.wxs` to:
- Change installation directory
- Add/remove files
- Modify service configuration
- Add custom actions
- Change UI

After modifications, rebuild:
```powershell
dotnet build HSPrint.Installer/HSPrint.Installer.wixproj -c Release
```

## Security Considerations

- The installer requires administrator privileges
- Service runs as LocalSystem for printer access
- Application listens only on localhost (127.0.0.1)
- CORS restricted to specified origins
- No remote administration capabilities

## Support

For issues or questions:
- GitHub Issues: https://github.com/HiddeS03/HSPrint/issues
- Documentation: https://github.com/HiddeS03/HSPrint/blob/master/README.md
