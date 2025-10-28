# HSPrint Build and Publish Script
# This script builds a self-contained Windows executable

Write-Host "HSPrint - Build and Publish Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Read version
$version = Get-Content version.txt -ErrorAction SilentlyContinue
if (-not $version) {
  $version = "1.0.0"
}
Write-Host "Version: $version" -ForegroundColor Green

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path ".\publish") {
  Remove-Item ".\publish" -Recurse -Force
}
if (Test-Path ".\artifacts") {
    Remove-Item ".\artifacts" -Recurse -Force
}

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore

# Build
Write-Host "Building..." -ForegroundColor Yellow
dotnet build --configuration Release

# Publish self-contained
Write-Host "Publishing self-contained executable..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish

# Create artifacts folder and zip
Write-Host "Creating ZIP archive..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path "./artifacts" | Out-Null
Compress-Archive -Path "./publish/*" -DestinationPath "./artifacts/HSPrint-$version.zip" -Force

Write-Host ""
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "Output: ./publish/" -ForegroundColor Cyan
Write-Host "Archive: ./artifacts/HSPrint-$version.zip" -ForegroundColor Cyan
Write-Host ""
Write-Host "To run the published version:" -ForegroundColor Yellow
Write-Host "  cd publish" -ForegroundColor White
Write-Host "  .\HSPrint.exe" -ForegroundColor White
