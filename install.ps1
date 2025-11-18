# HSPrint Installation Script
# Automates the installation process with proper cleanup and configuration

param(
    [Parameter(Mandatory=$false)]
    [string]$MsiPath = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$Silent = $false
)

# Ensure running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "" -ForegroundColor Cyan
Write-Host "  HSPrint Installation Script" -ForegroundColor Cyan
Write-Host "" -ForegroundColor Cyan
Write-Host ""

# Function to find MSI file
function Find-MsiFile {
    param([string]$ProvidedPath)
    
    if ($ProvidedPath -and (Test-Path $ProvidedPath)) {
        return $ProvidedPath
    }
    
    # Look for MSI in current directory
    $msiFiles = Get-ChildItem -Path "." -Filter "HSPrintSetup*.msi" -File | Sort-Object LastWriteTime -Descending
    
    if ($msiFiles.Count -eq 0) {
        Write-Host "ERROR: No HSPrint MSI installer found!" -ForegroundColor Red
        Write-Host "Please specify the MSI path: .\install.ps1 -MsiPath 'path\to\HSPrintSetup.msi'" -ForegroundColor Yellow
        return $null
    }
    
    if ($msiFiles.Count -eq 1) {
        return $msiFiles[0].FullName
    }
    
    # Multiple MSI files found
    if ($Silent) {
        return $msiFiles[0].FullName
    }
    
    Write-Host "Multiple MSI files found:" -ForegroundColor Yellow
    for ($i = 0; $i -lt $msiFiles.Count; $i++) {
        Write-Host "  [$($i + 1)] $($msiFiles[$i].Name)" -ForegroundColor White
    }
    
    $selection = Read-Host "Select MSI file (1-$($msiFiles.Count))"
    $index = [int]$selection - 1
    
    if ($index -ge 0 -and $index -lt $msiFiles.Count) {
        return $msiFiles[$index].FullName
    }
    
    return $null
}

# Function to stop HSPrint service
function Stop-HSPrintService {
    Write-Host "Checking for running HSPrint service..." -ForegroundColor Yellow
    
    $service = Get-Service -Name "HSPrintService" -ErrorAction SilentlyContinue
    if ($service -and $service.Status -eq 'Running') {
        Write-Host "  Stopping HSPrint service..." -ForegroundColor Yellow
        Stop-Service -Name "HSPrintService" -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-Host "   Service stopped" -ForegroundColor Green
    }
}

# Function to stop running HSPrint processes
function Stop-HSPrintProcesses {
    Write-Host "Checking for running HSPrint processes..." -ForegroundColor Yellow
    
    $processes = Get-Process -Name "HSPrint*" -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Host "  Stopping HSPrint processes..." -ForegroundColor Yellow
        foreach ($process in $processes) {
            try {
                $process.Kill()
                $process.WaitForExit(5000)
            } catch {
                # Ignore errors
            }
        }
        Start-Sleep -Seconds 1
        Write-Host "   Processes stopped" -ForegroundColor Green
    }
}

# Function to uninstall existing version
function Uninstall-ExistingVersion {
    Write-Host "Checking for existing HSPrint installation..." -ForegroundColor Yellow
    
    # Check registry for installed version
    $uninstallKey = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*" -ErrorAction SilentlyContinue |
                    Where-Object { $_.DisplayName -like "*HSPrint*" }
    
    if (-not $uninstallKey) {
        # Check 64-bit registry on 64-bit systems
        $uninstallKey = Get-ItemProperty -Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*" -ErrorAction SilentlyContinue |
                        Where-Object { $_.DisplayName -like "*HSPrint*" }
    }
    
    if ($uninstallKey) {
        Write-Host "  Found existing installation: $($uninstallKey.DisplayName)" -ForegroundColor Yellow
        Write-Host "  Uninstalling previous version..." -ForegroundColor Yellow
        
        $productCode = $uninstallKey.PSChildName
        $uninstallArgs = "/x $productCode /qn /norestart"
        
        $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $uninstallArgs -Wait -PassThru -NoNewWindow
        
        if ($process.ExitCode -eq 0) {
            Write-Host "   Previous version uninstalled" -ForegroundColor Green
            Start-Sleep -Seconds 2
        } else {
            Write-Host "  Warning: Uninstall returned exit code $($process.ExitCode)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  No existing installation found" -ForegroundColor Gray
    }
}

# Function to clear cache
function Clear-HSPrintCache {
    Write-Host "Clearing HSPrint cache..." -ForegroundColor Yellow
    
    $appDataPath = Join-Path $env:LOCALAPPDATA "HSPrint"
    if (Test-Path $appDataPath) {
        Remove-Item -Path $appDataPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "   Application data cleared" -ForegroundColor Green
    }
    
    $tempPath = Join-Path $env:TEMP "HSPrint"
    if (Test-Path $tempPath) {
        Remove-Item -Path $tempPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "   Temporary files cleared" -ForegroundColor Green
    }
}

# Function to install MSI
function Install-HSPrint {
    param([string]$MsiFilePath)
    
    Write-Host ""
    Write-Host "Installing HSPrint from: $MsiFilePath" -ForegroundColor Cyan
    Write-Host ""
    
    $installArgs = if ($Silent) {
        "/i `"$MsiFilePath`" /qn /norestart"
    } else {
        "/i `"$MsiFilePath`" /qb /norestart"
    }
    
    Write-Host "Starting installation..." -ForegroundColor Yellow
    $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $installArgs -Wait -PassThru -NoNewWindow
    
    if ($process.ExitCode -eq 0) {
        Write-Host " Installation completed successfully!" -ForegroundColor Green
        return $true
    } else {
        Write-Host "ERROR: Installation failed with exit code $($process.ExitCode)" -ForegroundColor Red
        return $false
    }
}

# Function to start the service
function Start-HSPrintService {
    Write-Host ""
    Write-Host "Starting HSPrint service..." -ForegroundColor Yellow
    
    Start-Sleep -Seconds 2
    
    $service = Get-Service -Name "HSPrintService" -ErrorAction SilentlyContinue
    if ($service) {
        Start-Service -Name "HSPrintService" -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        
        $service = Get-Service -Name "HSPrintService" -ErrorAction SilentlyContinue
        if ($service.Status -eq 'Running') {
            Write-Host " HSPrint service started successfully!" -ForegroundColor Green
        } else {
            Write-Host "Warning: Service installed but not running. You may need to start it manually." -ForegroundColor Yellow
        }
    } else {
        Write-Host "Warning: Service not found after installation." -ForegroundColor Yellow
    }
}

# Function to launch configuration tool
function Start-ConfigTool {
    Write-Host ""
    Write-Host "Launching HSPrint Configuration Tool..." -ForegroundColor Yellow
    
    $installPath = (Get-ItemProperty -Path "HKLM:\Software\HSPrint" -ErrorAction SilentlyContinue).InstallPath
    
    if ($installPath) {
        $configToolPath = Join-Path $installPath "HSPrint.ConfigTool.exe"
        if (Test-Path $configToolPath) {
            Start-Process -FilePath $configToolPath -ErrorAction SilentlyContinue
            Write-Host " Configuration tool launched!" -ForegroundColor Green
        }
    }
}

# Main installation process
try {
    # Step 1: Find MSI file
    $msiFile = Find-MsiFile -ProvidedPath $MsiPath
    if (-not $msiFile) {
        exit 1
    }
    
    Write-Host "Using MSI: $msiFile" -ForegroundColor White
    Write-Host ""
    
    if (-not $Silent) {
        $confirm = Read-Host "Continue with installation? (Y/N)"
        if ($confirm -ne 'Y' -and $confirm -ne 'y') {
            Write-Host "Installation cancelled." -ForegroundColor Yellow
            exit 0
        }
        Write-Host ""
    }
    
    # Step 2: Stop service and processes
    Stop-HSPrintService
    Stop-HSPrintProcesses
    
    # Step 3: Uninstall existing version
    Uninstall-ExistingVersion
    
    # Step 4: Clear cache
    Clear-HSPrintCache
    
    # Step 5: Install new version
    Write-Host ""
    $installSuccess = Install-HSPrint -MsiFilePath $msiFile
    
    if (-not $installSuccess) {
        exit 1
    }
    
    # Step 6: Start service
    Start-HSPrintService
    
    # Step 7: Launch configuration tool
    if (-not $Silent) {
        Start-ConfigTool
    }
    
    # Success message
    Write-Host ""
    Write-Host "" -ForegroundColor Green
    Write-Host "   HSPrint installed successfully!" -ForegroundColor Green
    Write-Host "" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "   HSPrint service is now running" -ForegroundColor White
    Write-Host "   Configuration tool is in your system tray" -ForegroundColor White
    Write-Host "   API available at: http://localhost:50246" -ForegroundColor White
    Write-Host "   Swagger docs at: http://localhost:50246/swagger" -ForegroundColor White
    Write-Host ""
    
    exit 0
    
} catch {
    Write-Host ""
    Write-Host "" -ForegroundColor Red
    Write-Host "  ERROR: Installation failed!" -ForegroundColor Red
    Write-Host "" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error details: `$(# HSPrint Installation Script
# Automates the installation process with proper cleanup and configuration

param(
    [Parameter(Mandatory=$false)]
    [string]$MsiPath = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$Silent = $false
)

# Ensure running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "" -ForegroundColor Cyan
Write-Host "  HSPrint Installation Script" -ForegroundColor Cyan
Write-Host "" -ForegroundColor Cyan
Write-Host ""

# Function to find MSI file
function Find-MsiFile {
    param([string]$ProvidedPath)
    
    if ($ProvidedPath -and (Test-Path $ProvidedPath)) {
        return $ProvidedPath
    }
    
    # Look for MSI in current directory
    $msiFiles = Get-ChildItem -Path "." -Filter "HSPrintSetup*.msi" -File | Sort-Object LastWriteTime -Descending
    
    if ($msiFiles.Count -eq 0) {
        Write-Host "ERROR: No HSPrint MSI installer found!" -ForegroundColor Red
        Write-Host "Please specify the MSI path: .\install.ps1 -MsiPath 'path\to\HSPrintSetup.msi'" -ForegroundColor Yellow
        return $null
    }
    
    if ($msiFiles.Count -eq 1) {
        return $msiFiles[0].FullName
    }
    
    # Multiple MSI files found
    if ($Silent) {
        return $msiFiles[0].FullName
    }
    
    Write-Host "Multiple MSI files found:" -ForegroundColor Yellow
    for ($i = 0; $i -lt $msiFiles.Count; $i++) {
        Write-Host "  [$($i + 1)] $($msiFiles[$i].Name)" -ForegroundColor White
    }
    
    $selection = Read-Host "Select MSI file (1-$($msiFiles.Count))"
    $index = [int]$selection - 1
    
    if ($index -ge 0 -and $index -lt $msiFiles.Count) {
        return $msiFiles[$index].FullName
    }
    
    return $null
}

# Function to stop HSPrint service
function Stop-HSPrintService {
    Write-Host "Checking for running HSPrint service..." -ForegroundColor Yellow
    
    $service = Get-Service -Name "HSPrintService" -ErrorAction SilentlyContinue
    if ($service -and $service.Status -eq 'Running') {
        Write-Host "  Stopping HSPrint service..." -ForegroundColor Yellow
        Stop-Service -Name "HSPrintService" -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-Host "   Service stopped" -ForegroundColor Green
    }
}

# Function to stop running HSPrint processes
function Stop-HSPrintProcesses {
    Write-Host "Checking for running HSPrint processes..." -ForegroundColor Yellow
    
    $processes = Get-Process -Name "HSPrint*" -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Host "  Stopping HSPrint processes..." -ForegroundColor Yellow
        foreach ($process in $processes) {
            try {
                $process.Kill()
                $process.WaitForExit(5000)
            } catch {
                # Ignore errors
            }
        }
        Start-Sleep -Seconds 1
        Write-Host "   Processes stopped" -ForegroundColor Green
    }
}

# Function to uninstall existing version
function Uninstall-ExistingVersion {
    Write-Host "Checking for existing HSPrint installation..." -ForegroundColor Yellow
    
    # Check registry for installed version
    $uninstallKey = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*" -ErrorAction SilentlyContinue |
                    Where-Object { $_.DisplayName -like "*HSPrint*" }
    
    if (-not $uninstallKey) {
        # Check 64-bit registry on 64-bit systems
        $uninstallKey = Get-ItemProperty -Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*" -ErrorAction SilentlyContinue |
                        Where-Object { $_.DisplayName -like "*HSPrint*" }
    }
    
    if ($uninstallKey) {
        Write-Host "  Found existing installation: $($uninstallKey.DisplayName)" -ForegroundColor Yellow
        Write-Host "  Uninstalling previous version..." -ForegroundColor Yellow
        
        $productCode = $uninstallKey.PSChildName
        $uninstallArgs = "/x $productCode /qn /norestart"
        
        $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $uninstallArgs -Wait -PassThru -NoNewWindow
        
        if ($process.ExitCode -eq 0) {
            Write-Host "   Previous version uninstalled" -ForegroundColor Green
            Start-Sleep -Seconds 2
        } else {
            Write-Host "  Warning: Uninstall returned exit code $($process.ExitCode)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  No existing installation found" -ForegroundColor Gray
    }
}

# Function to clear cache
function Clear-HSPrintCache {
    Write-Host "Clearing HSPrint cache..." -ForegroundColor Yellow
    
    $appDataPath = Join-Path $env:LOCALAPPDATA "HSPrint"
    if (Test-Path $appDataPath) {
        Remove-Item -Path $appDataPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "   Application data cleared" -ForegroundColor Green
    }
    
    $tempPath = Join-Path $env:TEMP "HSPrint"
    if (Test-Path $tempPath) {
        Remove-Item -Path $tempPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "   Temporary files cleared" -ForegroundColor Green
    }
}

# Function to install MSI
function Install-HSPrint {
    param([string]$MsiFilePath)
    
    Write-Host ""
    Write-Host "Installing HSPrint from: $MsiFilePath" -ForegroundColor Cyan
    Write-Host ""
    
    $installArgs = if ($Silent) {
        "/i `"$MsiFilePath`" /qn /norestart"
    } else {
        "/i `"$MsiFilePath`" /qb /norestart"
    }
    
    Write-Host "Starting installation..." -ForegroundColor Yellow
    $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $installArgs -Wait -PassThru -NoNewWindow
    
    if ($process.ExitCode -eq 0) {
        Write-Host " Installation completed successfully!" -ForegroundColor Green
        return $true
    } else {
        Write-Host "ERROR: Installation failed with exit code $($process.ExitCode)" -ForegroundColor Red
        return $false
    }
}

# Function to start the service
function Start-HSPrintService {
    Write-Host ""
    Write-Host "Starting HSPrint service..." -ForegroundColor Yellow
    
    Start-Sleep -Seconds 2
    
    $service = Get-Service -Name "HSPrintService" -ErrorAction SilentlyContinue
    if ($service) {
        Start-Service -Name "HSPrintService" -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        
        $service = Get-Service -Name "HSPrintService" -ErrorAction SilentlyContinue
        if ($service.Status -eq 'Running') {
            Write-Host " HSPrint service started successfully!" -ForegroundColor Green
        } else {
            Write-Host "Warning: Service installed but not running. You may need to start it manually." -ForegroundColor Yellow
        }
    } else {
        Write-Host "Warning: Service not found after installation." -ForegroundColor Yellow
    }
}

# Function to launch configuration tool
function Start-ConfigTool {
    Write-Host ""
    Write-Host "Launching HSPrint Configuration Tool..." -ForegroundColor Yellow
    
    $installPath = (Get-ItemProperty -Path "HKLM:\Software\HSPrint" -ErrorAction SilentlyContinue).InstallPath
    
    if ($installPath) {
        $configToolPath = Join-Path $installPath "HSPrint.ConfigTool.exe"
        if (Test-Path $configToolPath) {
            Start-Process -FilePath $configToolPath -ErrorAction SilentlyContinue
            Write-Host " Configuration tool launched!" -ForegroundColor Green
        }
    }
}

# Main installation process
try {
    # Step 1: Find MSI file
    $msiFile = Find-MsiFile -ProvidedPath $MsiPath
    if (-not $msiFile) {
        exit 1
    }
    
    Write-Host "Using MSI: $msiFile" -ForegroundColor White
    Write-Host ""
    
    if (-not $Silent) {
        $confirm = Read-Host "Continue with installation? (Y/N)"
        if ($confirm -ne 'Y' -and $confirm -ne 'y') {
            Write-Host "Installation cancelled." -ForegroundColor Yellow
            exit 0
        }
        Write-Host ""
    }
    
    # Step 2: Stop service and processes
    Stop-HSPrintService
    Stop-HSPrintProcesses
    
    # Step 3: Uninstall existing version
    Uninstall-ExistingVersion
    
    # Step 4: Clear cache
    Clear-HSPrintCache
    
    # Step 5: Install new version
    Write-Host ""
    $installSuccess = Install-HSPrint -MsiFilePath $msiFile
    
    if (-not $installSuccess) {
        exit 1
    }
    
    # Step 6: Start service
    Start-HSPrintService
    
    # Step 7: Launch configuration tool
    if (-not $Silent) {
        Start-ConfigTool
    }
    
    # Success message
    Write-Host ""
    Write-Host "" -ForegroundColor Green
    Write-Host "   HSPrint installed successfully!" -ForegroundColor Green
    Write-Host "" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "   HSPrint service is now running" -ForegroundColor White
    Write-Host "   Configuration tool is in your system tray" -ForegroundColor White
    Write-Host "   API available at: http://localhost:50246" -ForegroundColor White
    Write-Host "   Swagger docs at: http://localhost:50246/swagger" -ForegroundColor White
    Write-Host ""
    
    exit 0
    
} catch {
    Write-Host ""
    Write-Host "" -ForegroundColor Red
    Write-Host "  ERROR: Installation failed!" -ForegroundColor Red
    Write-Host "" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error details: $_" -ForegroundColor Red
    Write-Host ""
    exit 1
}
.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    exit 1
}
