# HSPrint Installation Script
# This script handles the installation of HSPrint with proper cleanup and configuration

param(
    [string]$MsiPath = "",
 [switch]$Silent = $false
)

$ErrorActionPreference = "Stop"

Write-Host "HSPrint Installation Script" -ForegroundColor Cyan
Write-Host "===========================" -ForegroundColor Cyan
Write-Host ""

# Check for admin privileges
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script requires administrator privileges." -ForegroundColor Red
    Write-Host "Please right-click and select 'Run as Administrator'" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Function to check if HSPrint is installed
function Test-HSPrintInstalled {
    $regPath = "HKLM:\Software\HSPrint"
    return Test-Path $regPath
}

# Function to get installed version
function Get-HSPrintVersion {
    $regPath = "HKLM:\Software\HSPrint"
    if (Test-Path $regPath) {
        $version = Get-ItemProperty -Path $regPath -Name "Version" -ErrorAction SilentlyContinue
        return $version.Version
    }
return $null
}

# Function to stop HSPrint processes
function Stop-HSPrint {
    Write-Host "Stopping HSPrint processes..." -ForegroundColor Yellow
 
    # Stop the service
    $service = Get-Service -Name "HSPrintService" -ErrorAction SilentlyContinue
    if ($service) {
        if ($service.Status -eq "Running") {
 Stop-Service -Name "HSPrintService" -Force
          Write-Host "  - Service stopped" -ForegroundColor Green
        }
    }
    
    # Stop any running processes
    $processes = Get-Process -Name "HSPrint" -ErrorAction SilentlyContinue
    if ($processes) {
        $processes | Stop-Process -Force
        Write-Host "  - Processes terminated" -ForegroundColor Green
    }
    
    Start-Sleep -Seconds 2
}

# Function to uninstall existing version
function Uninstall-HSPrint {
    Write-Host "Checking for existing installation..." -ForegroundColor Yellow
    
    if (-not (Test-HSPrintInstalled)) {
        Write-Host "  - No existing installation found" -ForegroundColor Green
        return $true
    }
    
    $currentVersion = Get-HSPrintVersion
    Write-Host "  - Found version: $currentVersion" -ForegroundColor Cyan
    Write-Host "Uninstalling existing version..." -ForegroundColor Yellow
    
    # Find the product code
    $uninstallKeys = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*"
  )
    
    $productCode = $null
    foreach ($key in $uninstallKeys) {
        $apps = Get-ItemProperty $key -ErrorAction SilentlyContinue
        foreach ($app in $apps) {
       if ($app.DisplayName -like "*HSPrint*") {
              $productCode = $app.PSChildName
 break
            }
      }
        if ($productCode) { break }
    }
    
    if ($productCode) {
        Write-Host "  - Uninstalling product code: $productCode" -ForegroundColor Cyan
  
        $uninstallArgs = "/x $productCode /qn /norestart"
        if (-not $Silent) {
            $uninstallArgs = "/x $productCode /passive /norestart"
      }
      
      $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $uninstallArgs -Wait -PassThru
    
  if ($process.ExitCode -eq 0 -or $process.ExitCode -eq 3010) {
Write-Host "  - Uninstall completed successfully" -ForegroundColor Green
            Start-Sleep -Seconds 3
        return $true
   } else {
         Write-Host "  - Uninstall returned exit code: $($process.ExitCode)" -ForegroundColor Yellow
            return $true  # Continue anyway
        }
    } else {
        Write-Host "  - Could not find product code, attempting manual cleanup" -ForegroundColor Yellow
        return $true
    }
}

# Function to clear cache
function Clear-HSPrintCache {
    Write-Host "Clearing application cache..." -ForegroundColor Yellow

    $cachePaths = @(
      "$env:LOCALAPPDATA\HSPrint",
    "$env:TEMP\HSPrint"
    )
    
    foreach ($path in $cachePaths) {
        if (Test-Path $path) {
        Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
  Write-Host "  - Cleared: $path" -ForegroundColor Green
   }
    }
}

# Function to install HSPrint
function Install-HSPrint {
    param([string]$MsiPath)
    
    Write-Host "Installing HSPrint..." -ForegroundColor Yellow
    
    if (-not (Test-Path $MsiPath)) {
 Write-Host "ERROR: MSI file not found: $MsiPath" -ForegroundColor Red
        return $false
    }
    
    $installArgs = "/i `"$MsiPath`" /qn /norestart /l*v `"$env:TEMP\HSPrint-Install.log`""
    if (-not $Silent) {
        $installArgs = "/i `"$MsiPath`" /passive /norestart /l*v `"$env:TEMP\HSPrint-Install.log`""
    }
    
    Write-Host "  - Installing from: $MsiPath" -ForegroundColor Cyan
    $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $installArgs -Wait -PassThru
    
    if ($process.ExitCode -eq 0 -or $process.ExitCode -eq 3010) {
        Write-Host "  - Installation completed successfully" -ForegroundColor Green
        return $true
    } else {
        Write-Host "ERROR: Installation failed with exit code: $($process.ExitCode)" -ForegroundColor Red
        Write-Host "Check log file: $env:TEMP\HSPrint-Install.log" -ForegroundColor Yellow
        return $false
    }
}

# Main installation flow
try {
    Write-Host "Step 1: Stopping running instances" -ForegroundColor Cyan
    Stop-HSPrint
    
    Write-Host ""
  Write-Host "Step 2: Removing existing installation" -ForegroundColor Cyan
    if (-not (Uninstall-HSPrint)) {
        throw "Failed to uninstall existing version"
    }
  
    Write-Host ""
    Write-Host "Step 3: Clearing cache" -ForegroundColor Cyan
    Clear-HSPrintCache
    
    Write-Host ""
    Write-Host "Step 4: Installing HSPrint" -ForegroundColor Cyan
    
 # Find MSI file if not specified
    if ([string]::IsNullOrEmpty($MsiPath)) {
  $msiFiles = Get-ChildItem -Path "." -Filter "HSPrintSetup*.msi" -File
 if ($msiFiles.Count -eq 0) {
 throw "No HSPrint MSI installer found in current directory"
}
        $MsiPath = $msiFiles[0].FullName
    }
    
  if (-not (Install-HSPrint -MsiPath $MsiPath)) {
        throw "Installation failed"
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
 Write-Host "HSPrint installed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
 Write-Host ""
    Write-Host "The HSPrint service has been configured to start automatically." -ForegroundColor Cyan
    Write-Host "HSPrint will also run on Windows startup." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Access the API at: http://localhost:50246" -ForegroundColor Yellow
    Write-Host "Documentation: http://localhost:50246/swagger" -ForegroundColor Yellow
    
    if (-not $Silent) {
        Read-Host "`nPress Enter to exit"
    }
    
 exit 0
}
catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Installation failed!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if (-not $Silent) {
  Read-Host "`nPress Enter to exit"
    }
    
    exit 1
}
