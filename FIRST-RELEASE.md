# Creating Your First HSPrint Release

This guide walks you through creating and publishing the first version of HSPrint.

## Prerequisites Checklist

- [ ] All code is committed and pushed to GitHub
- [ ] Version number is set in `version.txt` (e.g., `1.0.0`)
- [ ] WiX Toolset 4 is installed: `dotnet tool install --global wix --version 4.0.5`
- [ ] You have administrator rights on your development machine
- [ ] GitHub repository secrets are configured (if using automatic uploads)

## Step 1: Set the Version Number

Edit `version.txt` to set your release version:

```plaintext
1.0.0
```

## Step 2: Build the Installer Locally

### Option A: Using the Batch File (Easiest)

Simply double-click `build-installer.bat` or run:

```cmd
build-installer.bat
```

### Option B: Using PowerShell

```powershell
.\build-installer.ps1 -Version "1.0.0"
```

This will create files in the `artifacts/` directory:
- `HSPrintSetup-1.0.0.msi` - Windows installer
- `HSPrint-1.0.0.zip` - Portable archive
- `install.ps1` - Installation script

## Step 3: Test the Installer Locally

Before releasing, test the installation:

1. **Fresh Installation Test**
   ```powershell
   # Run the installer
   .\artifacts\HSPrintSetup-1.0.0.msi
   
   # Or use the script
   .\install.ps1 -MsiPath ".\artifacts\HSPrintSetup-1.0.0.msi"
   ```

2. **Verify Installation**
   ```powershell
 # Check if service is running
   Get-Service HSPrintService
   
   # Test the API
   curl http://localhost:50246/health
   
   # Check startup configuration
   Get-ItemProperty "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "HSPrint"
   ```

3. **Test Upgrade**
   - Change version.txt to `1.0.1`
   - Build again
   - Run the new installer
   - Verify it uninstalls the old version and installs the new one

4. **Test Uninstall**
   ```powershell
   # Via Control Panel
   # Open "Add or Remove Programs" ? Find "HSPrint" ? Uninstall
   
   # Or via PowerShell
   $app = Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\* | 
           Where-Object { $_.DisplayName -like "*HSPrint*" }
   msiexec /x $app.PSChildName /qn
   ```

## Step 4: Commit and Push Changes

```bash
# Add all changes
git add .

# Commit with a clear message
git commit -m "Release v1.0.0 - Initial release with MSI installer"

# Push to master
git push origin master
```

## Step 5: Create a Git Tag

Tags trigger the GitHub Actions workflow to create a release:

```bash
# Create an annotated tag
git tag -a v1.0.0 -m "Release version 1.0.0 - First stable release"

# Push the tag
git push origin v1.0.0
```

## Step 6: Monitor GitHub Actions

1. Go to your repository on GitHub
2. Click on the **Actions** tab
3. Watch the "Build and Release HSPrint" workflow run
4. Wait for it to complete (usually 5-10 minutes)

The workflow will:
- ? Build the application
- ? Create the MSI installer
- ? Create the ZIP archive
- ? Upload to Supabase Storage (if configured)
- ? Create a GitHub Release with downloadable files

## Step 7: Verify the GitHub Release

1. Go to your repository's **Releases** page
2. You should see "HSPrint v1.0.0"
3. Verify the following files are attached:
   - `HSPrintSetup-1.0.0.msi`
   - `HSPrint-1.0.0.zip`
   - `install.ps1`

## Step 8: Test the Release Download

Download and test the released files:

```powershell
# Download the MSI
Invoke-WebRequest -Uri "https://github.com/HiddeS03/HSPrint/releases/download/v1.0.0/HSPrintSetup-1.0.0.msi" `
          -OutFile "HSPrintSetup-1.0.0.msi"

# Install it
.\install.ps1 -MsiPath ".\HSPrintSetup-1.0.0.msi"

# Test
curl http://localhost:50246/health
```

## Step 9: Update Documentation

Update the README.md with release information:

```markdown
## Latest Release

Download the latest version: [HSPrint v1.0.0](https://github.com/HiddeS03/HSPrint/releases/tag/v1.0.0)

- MSI Installer: [HSPrintSetup-1.0.0.msi](https://github.com/HiddeS03/HSPrint/releases/download/v1.0.0/HSPrintSetup-1.0.0.msi)
- Portable ZIP: [HSPrint-1.0.0.zip](https://github.com/HiddeS03/HSPrint/releases/download/v1.0.0/HSPrint-1.0.0.zip)
```

## Step 10: Announce the Release

Consider announcing the release:

- Update your website
- Send notifications to users
- Post on social media
- Update documentation site

## Troubleshooting

### Build Fails on GitHub Actions

**Issue**: WiX Toolset not found
**Solution**: The workflow includes a step to install WiX. Check the Actions logs.

**Issue**: Build fails with missing dependencies
**Solution**: Ensure all project files are committed and pushed.

### MSI Installation Fails

**Issue**: "Another version is already installed"
**Solution**: The installer should handle this automatically. If not:
```powershell
# Manually uninstall
$app = Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\* | 
    Where-Object { $_.DisplayName -like "*HSPrint*" }
msiexec /x $app.PSChildName /qn /norestart

# Clear cache
Remove-Item "$env:LOCALAPPDATA\HSPrint" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "$env:TEMP\HSPrint" -Recurse -Force -ErrorAction SilentlyContinue

# Try again
.\install.ps1 -MsiPath "HSPrintSetup-1.0.0.msi"
```

### Service Won't Start

**Issue**: Service fails to start after installation
**Solution**: Check the Windows Event Log:
```powershell
Get-EventLog -LogName Application -Source HSPrintService -Newest 10
```

Check application logs:
```powershell
Get-Content "C:\Program Files\HSPrint\logs\hsprinteragent-$(Get-Date -Format 'yyyyMMdd').log"
```

### GitHub Actions Workflow Doesn't Trigger

**Issue**: Push tag but workflow doesn't run
**Solution**: 
1. Check that the tag matches the pattern in `.github/workflows/build.yml`
2. Ensure the workflow file is in the `master` or `main` branch
3. Check repository Actions settings (Actions must be enabled)

## Manual Release (Without GitHub Actions)

If you prefer to create releases manually:

1. **Build Locally**
   ```powershell
   .\build-installer.ps1 -Version "1.0.0"
   ```

2. **Create Release on GitHub**
   - Go to your repository
- Click "Releases" ? "Draft a new release"
   - Create tag: `v1.0.0`
   - Title: `HSPrint v1.0.0`
 - Description: Release notes
   - Upload files from `artifacts/`:
     - `HSPrintSetup-1.0.0.msi`
     - `HSPrint-1.0.0.zip`
     - `install.ps1`
   - Click "Publish release"

## Release Checklist

Before publishing, verify:

- [ ] Version number is correct in `version.txt`
- [ ] All tests pass
- [ ] MSI installs correctly
- [ ] Service starts automatically
- [ ] Application runs on http://localhost:50246
- [ ] Swagger UI is accessible
- [ ] All API endpoints work
- [ ] Upgrade from previous version works (if applicable)
- [ ] Uninstall works cleanly
- [ ] README is updated
- [ ] CHANGELOG is updated (if you have one)
- [ ] Release notes are written

## Next Steps

After your first release:

1. **Monitor for Issues**
   - Watch GitHub issues
   - Monitor error logs
   - Collect user feedback

2. **Plan Updates**
   - Create a roadmap
   - Track feature requests
   - Plan bug fix releases

3. **Set Up Auto-Update**
   - Configure your backend to serve version information
   - Test the auto-update mechanism
   - Document the update process for users

## Future Releases

For subsequent releases:

1. Update `version.txt` (e.g., `1.0.1`, `1.1.0`, `2.0.0`)
2. Commit changes
3. Create and push tag
4. GitHub Actions handles the rest!

```bash
# Quick release commands
echo "1.1.0" > version.txt
git add version.txt
git commit -m "Bump version to 1.1.0"
git push origin master
git tag -a v1.1.0 -m "Release 1.1.0"
git push origin v1.1.0
```

## Support

If you encounter issues:
- Check [INSTALLER.md](INSTALLER.md) for detailed installation documentation
- Review GitHub Actions logs
- Check Windows Event Viewer
- Open an issue on GitHub

---

**Congratulations on your first release! ??**
