# HSPrint Installer Build Script
# Builds the application and creates an MSI installer

param(
    [string]$Configuration = "Release",
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

Write-Host "HSPrint Installer Build Script" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
Write-Host ""

# Get version from version.txt if not specified
if ([string]::IsNullOrEmpty($Version)) {
    if (Test-Path "version.txt") {
        $Version = (Get-Content "version.txt").Trim()
        Write-Host "Using version from version.txt: $Version" -ForegroundColor Green
    } else {
        $Version = "1.0.0"
        Write-Host "Using default version: $Version" -ForegroundColor Yellow
    }
}

# Update version in version.txt
Set-Content -Path "version.txt" -Value $Version -NoNewline

Write-Host "Building HSPrint v$Version..." -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean previous builds
Write-Host "Step 1: Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "artifacts") {
 Remove-Item -Path "artifacts" -Recurse -Force
}
New-Item -ItemType Directory -Path "artifacts" -Force | Out-Null

# Step 2: Restore dependencies
Write-Host ""
Write-Host "Step 2: Restoring dependencies..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    throw "Failed to restore dependencies"
}

# Step 3: Build main application
Write-Host ""
Write-Host "Step 3: Building application..." -ForegroundColor Yellow
dotnet build HSPrint.csproj -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "Failed to build application"
}

# Step 4: Publish application
Write-Host ""
Write-Host "Step 4: Publishing application..." -ForegroundColor Yellow
$publishDir = "bin\$Configuration\net8.0-windows10.0.26100.0\win-x64\publish"
dotnet publish HSPrint.csproj `
    -c $Configuration `
  -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:Version=$Version `
    -o "./$publishDir"
if ($LASTEXITCODE -ne 0) {
    throw "Failed to publish application"
}

# Step 5: Generate WiX file list
Write-Host ""
Write-Host "Step 5: Generating WiX file list..." -ForegroundColor Yellow
& ".\HSPrint.Installer\GenerateFileList.ps1" -PublishDir $publishDir -OutputFile "HSPrint.Installer\GeneratedFiles.wxs"
if ($LASTEXITCODE -ne 0) {
    Write-Host "  - Warning: File list generation had issues, continuing..." -ForegroundColor Yellow
}

# Step 6: Build installer (if WiX is available)
Write-Host ""
Write-Host "Step 6: Building MSI installer..." -ForegroundColor Yellow

# Check if WiX is installed
$wixInstalled = $false
try {
    $wixPath = Get-Command "wix.exe" -ErrorAction SilentlyContinue
    if ($wixPath) {
        $wixInstalled = $true
    }
} catch {
  $wixInstalled = $false
}

if ($wixInstalled) {
    Write-Host "  - WiX Toolset found, building MSI..." -ForegroundColor Green
    
    # Build the installer project
    dotnet build HSPrint.Installer/HSPrint.Installer.wixproj `
        -c $Configuration `
        -p:Version=$Version
        
    if ($LASTEXITCODE -eq 0) {
# Copy MSI to artifacts
        $msiFile = Get-ChildItem -Path "HSPrint.Installer/bin/$Configuration" -Filter "*.msi" -Recurse | Select-Object -First 1
        if ($msiFile) {
      Copy-Item $msiFile.FullName -Destination "artifacts/HSPrintSetup-$Version.msi"
            Write-Host "  - MSI created: artifacts/HSPrintSetup-$Version.msi" -ForegroundColor Green
   }
    } else {
        Write-Host "  - MSI build failed, check errors above" -ForegroundColor Red
  throw "MSI build failed"
    }
} else {
    Write-Host "  - WiX Toolset not found, skipping MSI creation" -ForegroundColor Yellow
    Write-Host "  - Install WiX: dotnet tool install --global wix" -ForegroundColor Cyan
}

# Step 7: Copy install script
Write-Host ""
Write-Host "Step 7: Copying install script..." -ForegroundColor Yellow
Copy-Item "install.ps1" -Destination "artifacts/install.ps1"
Write-Host "  - install.ps1 copied to artifacts/" -ForegroundColor Green

# Step 8: Summary
Write-Host ""
Write-Host "==============================" -ForegroundColor Green
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "==============================" -ForegroundColor Green
Write-Host ""
Write-Host "Artifacts:" -ForegroundColor Cyan
Get-ChildItem "artifacts" -File | ForEach-Object {
    $size = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  - $($_.Name) ($size MB)" -ForegroundColor White
}

Write-Host ""
Write-Host "To install, run: .\install.ps1 -MsiPath '.\artifacts\HSPrintSetup-$Version.msi'" -ForegroundColor Yellow
