# Quick installer launcher - automatically runs as Administrator
# Usage: .\install-elevated.ps1

$msiPath = "artifacts\HSPrintSetup-1.0.0.msi"

if (-not (Test-Path $msiPath)) {
    Write-Host "ERROR: MSI not found at $msiPath" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "Restarting as Administrator..." -ForegroundColor Yellow
    $scriptPath = $MyInvocation.MyCommand.Path
    Start-Process powershell.exe -Verb RunAs -ArgumentList "-NoExit", "-ExecutionPolicy Bypass", "-File `"$scriptPath`""
    exit
}

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  HSPrint Installation" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host ""

# Stop existing service if running
Write-Host "Checking for existing service..." -ForegroundColor Yellow
try {
    $service = Get-Service -Name "HSPrintService" -ErrorAction SilentlyContinue
    if ($service) {
        if ($service.Status -eq 'Running') {
            Write-Host "Stopping existing service..." -ForegroundColor Yellow
            Stop-Service -Name "HSPrintService" -Force
            Start-Sleep -Seconds 2
        }
    }
}
catch {
    # Service doesn't exist yet, that's fine
}

# Install MSI
Write-Host "Installing HSPrint..." -ForegroundColor Yellow
$logPath = Join-Path $PWD "install-log.txt"
$fullMsiPath = Join-Path $PWD $msiPath

$process = Start-Process msiexec.exe -ArgumentList "/i `"$fullMsiPath`" /qb /l*v `"$logPath`"" -Wait -PassThru

if ($process.ExitCode -eq 0) {
    Write-Host ""
    Write-Host "================================================================" -ForegroundColor Green
    Write-Host "  Installation Successful!" -ForegroundColor Green
    Write-Host "================================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "HSPrint service is now installed and running." -ForegroundColor Green
    Write-Host "Config tool should appear in your system tray." -ForegroundColor Green
    Write-Host ""
    Write-Host "Service Status:" -ForegroundColor Cyan
    Get-Service -Name "HSPrintService" | Format-Table -AutoSize
    Write-Host ""
    Write-Host "API URL: http://localhost:50246" -ForegroundColor Cyan
    Write-Host "Swagger: http://localhost:50246/swagger" -ForegroundColor Cyan
    Write-Host ""
}
else {
    Write-Host ""
    Write-Host "================================================================" -ForegroundColor Red
    Write-Host "  Installation Failed (Exit Code: $($process.ExitCode))" -ForegroundColor Red
    Write-Host "================================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Check the log file for details: $logPath" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Last 30 lines of log:" -ForegroundColor Yellow
    Get-Content $logPath | Select-Object -Last 30
}

Write-Host ""
Read-Host "Press Enter to exit"
