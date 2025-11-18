# Quick Start Script for HSPrint Development
# This script helps you get started quickly with development

Write-Host "ðŸš€ HSPrint Quick Start" -ForegroundColor Cyan
Write-Host "=====================`n" -ForegroundColor Cyan

# Check if .NET SDK is installed
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "âœ… .NET SDK $dotnetVersion found" -ForegroundColor Green
} else {
    Write-Host "âŒ .NET SDK not found. Please install .NET 8.0 SDK" -ForegroundColor Red
    Write-Host "   Download from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

# Restore packages
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "âœ… Packages restored successfully" -ForegroundColor Green
} else {
    Write-Host "âŒ Failed to restore packages" -ForegroundColor Red
    exit 1
}

# Build project
Write-Host "`nBuilding project..." -ForegroundColor Yellow
dotnet build --no-restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "âœ… Build successful" -ForegroundColor Green
} else {
    Write-Host "âŒ Build failed" -ForegroundColor Red
    exit 1
}

Write-Host "`nâœ¨ All set! Here's what you can do next:`n" -ForegroundColor Cyan
Write-Host "  1. Press F5 to start debugging" -ForegroundColor White
Write-Host "  2. Press Ctrl+Shift+B to build" -ForegroundColor White
Write-Host "  3. Run 'dotnet run' to start the API" -ForegroundColor White
Write-Host "  4. Visit http://localhost:50246/swagger for API docs`n" -ForegroundColor White

Write-Host "ðŸ“– Quick Reference: See VSCODE-QUICKSTART.md" -ForegroundColor Yellow
Write-Host "ðŸ“š Full Guide: See .vscode/README.md`n" -ForegroundColor Yellow

# Ask if user wants to start the app
$response = Read-Host "Would you like to start the application now? (y/n)"
if ($response -eq 'y' -or $response -eq 'Y') {
    Write-Host "`nðŸš€ Starting HSPrint..." -ForegroundColor Green
    dotnet run
}
