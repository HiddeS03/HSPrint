# WiX Installer Build Fix

## Issues Fixed

The WiX installer had several build errors that have been resolved:

### 1. **Wildcard File Patterns Not Allowed**
**Error:** `WIX0027: The File/@Source attribute's value, '*.dll', is not a valid filename`

**Fix:** WiX 4 does not support wildcard patterns in File elements. Instead, we now:
- List essential files explicitly in `Product.wxs`
- Generate a component group dynamically for all other files using `GenerateFileList.ps1`

### 2. **Missing WiX Util Extension**
**Error:** `WIX0200: The ServiceInstall element contains an unhandled extension element 'ServiceConfig'`

**Fix:** Added the required package reference to `HSPrint.Installer.wixproj`:
```xml
<PackageReference Include="WixToolset.Util.wixext" Version="4.0.5" />
```

## How It Works Now

### Build Process

1. **Publish Application** - Creates all files in the publish directory
2. **Generate File List** - `GenerateFileList.ps1` scans the publish directory and creates `GeneratedFiles.wxs`
3. **Build Installer** - WiX compiles the installer including all files

### File Structure

**Product.wxs** - Main installer definition with:
- Essential files (exe, config files)
- Service installation
- Startup configuration
- Registry keys

**GeneratedFiles.wxs** - Auto-generated file containing:
- All DLL dependencies
- Runtime files
- Other supporting files

### Building the Installer

```powershell
# Simple build
.\build-installer.bat

# Or with PowerShell
.\build-installer.ps1 -Version "1.0.0"
```

The build script automatically:
? Publishes the application
? Generates the file list
? Builds the MSI installer
? Copies files to `artifacts/`

## What's Included in the Installer

The MSI installer now includes:
- ? HSPrint.exe (main application)
- ? All .NET runtime DLLs
- ? All NuGet package DLLs (Serilog, Swashbuckle, etc.)
- ? Configuration files (appsettings.json)
- ? Version file (version.txt)
- ? Windows Service configuration
- ? Startup registry entries
- ? Start menu shortcuts

## Testing

After building, test the installer:

```powershell
# Install
.\artifacts\HSPrintSetup-1.0.0.msi

# Verify service
Get-Service HSPrintService

# Test API
curl http://localhost:50246/health

# Check installed files
Get-ChildItem "C:\Program Files\HSPrint"
```

## Troubleshooting

### Build fails with "WiX not found"
```powershell
dotnet tool install --global wix --version 4.0.5
```

### GeneratedFiles.wxs not created
- Check that the publish directory exists
- Run `dotnet publish` manually first
- Check PowerShell execution policy

### Missing DLLs after installation
- Verify `GeneratedFiles.wxs` was created
- Check that it's included in the build
- Look in `HSPrint.Installer/bin/Release/` for the compiled MSI

## Next Steps

The installer is now ready for:
- Local testing
- GitHub Actions automated builds
- Release creation

Follow [FIRST-RELEASE.md](FIRST-RELEASE.md) to create your first release!
