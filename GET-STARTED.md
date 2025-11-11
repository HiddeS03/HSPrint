# Getting Started with HSPrint Installation & Release System

Welcome! Your HSPrint project now has a complete installation and release system. This document will help you get started.

## ?? What's Been Added

### New Files Created

1. **Installer Project**
   - `HSPrint.Installer/HSPrint.Installer.wixproj` - WiX installer project
   - `HSPrint.Installer/Product.wxs` - Installer configuration
   - `HSPrint.Installer/License.rtf` - License agreement for installer

2. **Helper Classes**
   - `InstallHelper.cs` - Installation detection, uninstall, and startup configuration

3. **Build Scripts**
   - `build-installer.ps1` - PowerShell script to build the installer
   - `build-installer.bat` - Batch file wrapper for easy building
   - `install.ps1` - Installation script with auto-cleanup

4. **Documentation**
   - `INSTALLER.md` - Complete installer documentation
   - `FIRST-RELEASE.md` - Step-by-step guide for your first release
   - `QUICK-REFERENCE.md` - Command reference card
   - `RELEASE-CHECKLIST.md` - Comprehensive pre-release checklist

5. **Updated Files**
   - `.github/workflows/build.yml` - Enhanced to build and release installers
   - `Program.cs` - Added installation check on startup
   - `README.md` - Updated with installation instructions

## ?? Quick Start (5 Minutes to First Build)

### Step 1: Install WiX Toolset (One-time setup)

```powershell
dotnet tool install --global wix --version 4.0.5
```

### Step 2: Build Your First Installer

Simply double-click:
```
build-installer.bat
```

Or run in PowerShell:
```powershell
.\build-installer.ps1 -Version "1.0.0"
```

### Step 3: Test the Installer

```powershell
# Install
.\artifacts\HSPrintSetup-1.0.0.msi

# Test
curl http://localhost:50246/health

# Check service
Get-Service HSPrintService
```

? **That's it!** You now have a working installer.

## ?? Creating Your First Release

Follow these simple steps:

### Option 1: Automated (Recommended)

```bash
# 1. Make sure version.txt is set to 1.0.0
echo "1.0.0" > version.txt

# 2. Commit your changes
git add .
git commit -m "Release v1.0.0 - Initial release"
git push origin master

# 3. Create and push a tag
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

GitHub Actions will automatically:
- ? Build the application
- ? Create the MSI installer
- ? Create a ZIP archive
- ? Create a GitHub Release
- ? Upload all files

### Option 2: Manual

1. Build locally: `.\build-installer.bat`
2. Go to GitHub ? Releases ? "Draft a new release"
3. Create tag `v1.0.0`
4. Upload files from `artifacts/` folder
5. Publish!

## ?? Key Features of Your New Installer

### For End Users

? **One-Click Installation** - Double-click MSI and go  
? **Automatic Startup** - Runs when Windows starts  
? **Windows Service** - Runs as a background service  
? **Auto-Update Support** - Upgrades automatically  
? **Clean Uninstall** - Removes everything properly  

### For You (Developer)

? **Auto-Uninstall** - Removes old versions automatically  
? **Cache Clearing** - Ensures clean installation
? **Registry Integration** - Proper Windows integration  
? **GitHub Actions** - Automated builds and releases  
? **Professional Packaging** - MSI installer standard  

## ?? Documentation Guide

Here's what to read when:

### **Right Now** (Getting Started)
1. **This file** - Overview (you are here)
2. **FIRST-RELEASE.md** - Create your first release

### **When Building Locally**
- **build-installer.ps1** - Just run it!
- **QUICK-REFERENCE.md** - Common commands

### **When Creating a Release**
- **FIRST-RELEASE.md** - Step-by-step guide
- **RELEASE-CHECKLIST.md** - Don't forget anything!

### **For Detailed Information**
- **INSTALLER.md** - Deep dive into the installer
- **README.md** - Full project documentation

### **For Troubleshooting**
- **QUICK-REFERENCE.md** - Common fixes
- **INSTALLER.md** - Troubleshooting section

## ?? How It Works

### Installation Process

1. **User runs MSI or install.ps1**
2. **Script checks for existing installation**
   - If found: Stops service ? Uninstalls ? Clears cache
3. **Installs new version**
   - Copies files to `C:\Program Files\HSPrint`
   - Creates Windows Service
   - Configures startup
   - Creates registry keys
4. **Starts the service**
5. **Done!** App is running on http://localhost:50246

### Upgrade Process

Your installer is **upgrade-aware**:

1. Detects existing installation
2. **Automatically uninstalls old version**
3. **Clears cache** to prevent conflicts
4. Installs new version
5. User doesn't need to manually uninstall!

### Startup Configuration

The application is configured to run on Windows startup in **two ways**:

1. **Windows Service** - Runs in background (primary method)
2. **Startup Registry Key** - Fallback method

Both are configured automatically by the installer.

## ?? Customization

### Change Installation Directory

Edit `HSPrint.Installer/Product.wxs`:
```xml
<StandardDirectory Id="ProgramFilesFolder">
  <Directory Id="INSTALLFOLDER" Name="YourAppName">
```

### Change Version

Simply update `version.txt`:
```
1.0.0
```

Everything else updates automatically!

### Change Service Name

Edit `HSPrint.Installer/Product.wxs`:
```xml
<ServiceInstall Id="HSPrintService"
   Name="YourServiceName"
       DisplayName="Your Service Display Name"
```

### Change Startup Behavior

Edit `HSPrint.Installer/Product.wxs`:
```xml
<!-- To disable automatic startup, remove or comment out: -->
<Component Id="StartupRegistry" ...>
```

## ?? Common Issues & Solutions

### "WiX not found" when building

**Solution:**
```powershell
dotnet tool install --global wix --version 4.0.5
```

### "Another version is already installed"

**Solution:** Use the install script, it handles this automatically:
```powershell
.\install.ps1 -MsiPath "HSPrintSetup-1.0.0.msi"
```

### Service won't start

**Check logs:**
```powershell
Get-Content "C:\Program Files\HSPrint\logs\hsprinteragent-$(Get-Date -Format 'yyyyMMdd').log"
```

### Port 50246 already in use

**Find what's using it:**
```powershell
Get-NetTCPConnection -LocalPort 50246
```

### GitHub Actions workflow doesn't trigger

**Check:**
1. Tag format: Must be `v1.0.0` (with 'v')
2. Branch: Workflow file must be in master/main
3. Actions enabled: Check repository settings

## ?? Next Steps

### Immediate (Next 10 minutes)
1. ? Read FIRST-RELEASE.md
2. ? Build your first installer locally
3. ? Test the installation on your machine

### Today
1. ? Create your first GitHub release (follow FIRST-RELEASE.md)
2. ? Test downloading and installing from GitHub
3. ? Verify the auto-update mechanism (if implemented)

### This Week
1. ? Test on different Windows versions
2. ? Get feedback from beta testers
3. ? Update documentation with any findings

### Ongoing
1. ? Follow RELEASE-CHECKLIST.md for each release
2. ? Monitor GitHub issues
3. ? Keep dependencies updated
4. ? Improve based on user feedback

## ?? Best Practices

### Version Numbering
- **Major.Minor.Patch** (e.g., 1.0.0)
- Major (1.x.x): Breaking changes
- Minor (x.1.x): New features
- Patch (x.x.1): Bug fixes

### Release Frequency
- **Patch releases**: As needed for bugs
- **Minor releases**: Every 2-4 weeks for features
- **Major releases**: When breaking changes needed

### Testing
- Always test installer on clean VM
- Test upgrade from previous version
- Verify uninstall is clean

### Documentation
- Update README for each release
- Keep CHANGELOG.md current
- Document breaking changes prominently

## ?? Getting Help

If you get stuck:

1. **Check QUICK-REFERENCE.md** - Common commands and fixes
2. **Check INSTALLER.md** - Detailed troubleshooting
3. **Check GitHub Actions logs** - Build failures
4. **Check Windows Event Viewer** - Service issues
5. **Check application logs** - Runtime errors

## ?? Support Resources

- **Documentation**: All the .md files in your project
- **GitHub Issues**: For bugs and feature requests
- **Build Logs**: `.github/workflows/build.yml` execution logs
- **Application Logs**: `C:\Program Files\HSPrint\logs\`
- **Windows Event Log**: Application log for service issues

## ? Success Checklist

You're ready to release when you can check all these:

- [ ] WiX Toolset is installed
- [ ] You can build the installer locally
- [ ] Installer installs on your machine
- [ ] Service starts and runs
- [ ] API responds at http://localhost:50246
- [ ] Swagger UI is accessible
- [ ] Application starts on Windows startup
- [ ] You can uninstall cleanly
- [ ] You understand the release process

## ?? Congratulations!

Your HSPrint project now has:

? Professional Windows installer (MSI)  
? Automatic uninstall of old versions  
? Windows Service integration  
? Automatic startup configuration  
? Automated GitHub releases  
? Complete documentation  
? Build and install scripts  
? Release checklists  

**You're ready to ship! ??**

---

## Quick Commands Summary

```powershell
# Build installer
.\build-installer.bat

# Install
.\install.ps1

# Test
curl http://localhost:50246/health

# Create release
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0

# Check service
Get-Service HSPrintService

# View logs
Get-Content "C:\Program Files\HSPrint\logs\hsprinteragent-$(Get-Date -Format 'yyyyMMdd').log" -Tail 20
```

---

**Ready to create your first release?**  
?? Go to [FIRST-RELEASE.md](FIRST-RELEASE.md) and follow the step-by-step guide!
