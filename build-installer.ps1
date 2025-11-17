# Build HSPrint MSI Installer
# This script builds both the main application and configuration tool, then creates an MSI installer

param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release"
)

Write-Host "Building HSPrint Installer v$Version" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration $Configuration
if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" }
if (Test-Path "HSPrint.ConfigTool/bin") { Remove-Item -Recurse -Force "HSPrint.ConfigTool/bin" }
if (Test-Path "HSPrint.Installer/bin") { Remove-Item -Recurse -Force "HSPrint.Installer/bin" }

# Publish directories
$publishDir = "bin/$Configuration/net8.0-windows10.0.26100.0/win-x64/publish"
$configToolPublishDir = "bin/$Configuration/net8.0-windows10.0.26100.0/win-x64/configtool"

Write-Host ""
Write-Host "Step 1: Publishing HSPrint Service..." -ForegroundColor Yellow
dotnet publish HSPrint.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:Version=$Version `
    -o "./$publishDir"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish HSPrint service"
    exit 1
}

Write-Host "✓ HSPrint Service published successfully" -ForegroundColor Green
Write-Host ""

Write-Host "Step 2: Publishing HSPrint Configuration Tool..." -ForegroundColor Yellow
dotnet publish HSPrint.ConfigTool/HSPrint.ConfigTool.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:Version=$Version `
    -o "./$configToolPublishDir"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to publish Configuration Tool"
    exit 1
}

Write-Host "✓ Configuration Tool published successfully" -ForegroundColor Green
Write-Host ""

Write-Host "Step 3: Generating WiX file lists..." -ForegroundColor Yellow
& ".\HSPrint.Installer\GenerateWixFiles.ps1" `
    -PublishDir $publishDir `
    -ConfigToolPublishDir $configToolPublishDir `
    -OutputDir "HSPrint.Installer"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to generate WiX file lists"
    exit 1
}

Write-Host "✓ WiX file lists generated successfully" -ForegroundColor Green
Write-Host ""

Write-Host "Step 4: Building MSI Installer..." -ForegroundColor Yellow
dotnet build HSPrint.Installer/HSPrint.Installer.wixproj `
    -c $Configuration `
    -p:Version=$Version

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build MSI installer"
    exit 1
}

Write-Host "✓ MSI Installer built successfully" -ForegroundColor Green
Write-Host ""

# Find and display MSI location
$msi = Get-ChildItem -Path "HSPrint.Installer/bin/$Configuration" -Filter "*.msi" -Recurse | Select-Object -First 1
if ($msi) {
    $outputDir = "artifacts"
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir | Out-Null
    }
    
    $finalMsiPath = "$outputDir/HSPrintSetup-$Version.msi"
    Copy-Item $msi.FullName -Destination $finalMsiPath -Force
    
    Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host "✓ BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Green
    Write-Host ""
    Write-Host "MSI Installer created:" -ForegroundColor Cyan
    Write-Host "  $finalMsiPath" -ForegroundColor White
    Write-Host ""
    $msiFile = Get-Item $finalMsiPath
    Write-Host "  Size: $([math]::Round($msiFile.Length / 1MB, 2)) MB" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Error "MSI file not found after build!"
    exit 1
}
