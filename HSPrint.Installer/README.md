# HSPrint Installer

This directory contains the WiX Toolset v4 installer project for HSPrint.

## File Structure

- **Product.wxs**: Main installer definition with core components, service installation, and feature configuration
- **GenerateWixFiles.ps1**: PowerShell script that generates WiX component groups from published files
- **GeneratedSharedFiles.wxs**: Auto-generated component group for shared .NET runtime files (generated during build)
- **GeneratedFiles.wxs**: Auto-generated component group for HSPrint service-specific files (generated during build)
- **GeneratedConfigToolFiles.wxs**: Auto-generated component group for ConfigTool-specific files (generated during build)
- **License.rtf**: License agreement shown during installation
- **HSPrint.Installer.wixproj**: MSBuild project file for the installer

## Building the Installer

### Prerequisites
- .NET 8.0 SDK
- WiX Toolset v4.0.5 or later (`dotnet tool install --global wix`)

### Build Process

1. **Publish Applications**: The build script first publishes both HSPrint service and ConfigTool as self-contained applications
2. **Generate WiX Files**: The `GenerateWixFiles.ps1` script analyzes published files and creates three component groups:
   - **SharedRuntimeFiles**: .NET runtime files used by both applications (e.g., coreclr.dll, System.*.dll)
   - **PublishedFiles**: HSPrint service-specific files
   - **ConfigToolFiles**: ConfigTool-specific files
3. **Build MSI**: WiX compiles all sources into a single MSI installer

### Using the Build Scripts

**PowerShell (Recommended):**
```powershell
.\build-installer.ps1 -Version "1.0.0" -Configuration Release
```

**GitHub Actions:**
The `.github/workflows/build.yml` workflow automatically builds the installer on push to main/master.

## File Generation Details

### Why Separate Shared Files?

Both HSPrint service and ConfigTool are self-contained .NET applications that share common runtime files. Previously, these shared files were included in both component groups, causing WiX to generate duplicate GUIDs and build warnings.

The new approach:
1. Identifies files that exist in both publish directories
2. Places shared files in a single `SharedRuntimeFiles` component group
3. Keeps application-specific files in their respective component groups
4. Eliminates duplicate GUID warnings and ensures proper installer behavior

### Manual File Generation

If you need to regenerate the WiX files manually:

```powershell
.\HSPrint.Installer\GenerateWixFiles.ps1 `
    -PublishDir "bin\Release\net8.0-windows10.0.26100.0\win-x64\publish" `
    -ConfigToolPublishDir "bin\Release\net8.0-windows10.0.26100.0\win-x64\configtool" `
    -OutputDir "HSPrint.Installer"
```

## Installer Features

The MSI installer includes:
- HSPrint service installation and configuration
- Windows service auto-start configuration
- ConfigTool with system tray integration
- Start menu shortcuts
- Automatic upgrade of previous versions
- Clean uninstallation

## Troubleshooting

### Build Warnings About Duplicate GUIDs
If you see warnings about duplicate component GUIDs, ensure you're using the latest `GenerateWixFiles.ps1` script. The old `GenerateFileList.ps1` and `GenerateConfigToolFiles.ps1` scripts are deprecated and should not be used.

### Missing Generated Files
The `Generated*.wxs` files are created during the build process. If they're missing:
1. Ensure applications are published first
2. Run the `GenerateWixFiles.ps1` script manually
3. Check that publish directories exist and contain files

### WiX Build Errors
- Verify WiX Toolset v4 is installed: `wix --version`
- Ensure all source paths in generated files are correct
- Check that all component groups are referenced in Product.wxs
