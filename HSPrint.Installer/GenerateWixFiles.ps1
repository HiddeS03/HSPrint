# Generate WiX file lists with proper handling of shared files
# This script creates three WiX fragments:
# 1. Shared runtime files (used by both applications)
# 2. HSPrint service-specific files
# 3. ConfigTool-specific files

param(
    [string]$PublishDir = "bin\Release\net8.0-windows10.0.26100.0\win-x64\publish",
    [string]$ConfigToolPublishDir = "bin\Release\net8.0-windows10.0.26100.0\win-x64\configtool",
    [string]$OutputDir = "HSPrint.Installer"
)

Write-Host "Generating WiX file lists..." -ForegroundColor Cyan
Write-Host "  Publish Dir: $PublishDir" -ForegroundColor Gray
Write-Host "  ConfigTool Dir: $ConfigToolPublishDir" -ForegroundColor Gray
Write-Host ""

if (-not (Test-Path $PublishDir)) {
    Write-Error "Publish directory not found: $PublishDir"
    exit 1
}

if (-not (Test-Path $ConfigToolPublishDir)) {
    Write-Error "ConfigTool publish directory not found: $ConfigToolPublishDir"
    exit 1
}

# Files excluded from all generated lists (manually included in Product.wxs)
$excludeFiles = @(
    "HSPrint.exe",
    "HSPrint.dll",
    "HSPrint.pdb",
    "HSPrint.deps.json",
    "HSPrint.runtimeconfig.json",
    "appsettings.json",
    "appsettings.Development.json",
    "version.txt",
    "HSPrint.ConfigTool.exe",
    "HSPrint.ConfigTool.dll",
    "HSPrint.ConfigTool.pdb",
    "HSPrint.ConfigTool.deps.json",
    "HSPrint.ConfigTool.runtimeconfig.json"
)

# Get all files from both directories
$publishFiles = Get-ChildItem -Path $PublishDir -File | Where-Object { 
    $excludeFiles -notcontains $_.Name 
} | Select-Object Name, Length

$configToolFiles = Get-ChildItem -Path $ConfigToolPublishDir -File | Where-Object { 
    $excludeFiles -notcontains $_.Name 
} | Select-Object Name, Length

Write-Host "Processing files..." -ForegroundColor Yellow
Write-Host "  Total files in publish: $($publishFiles.Count)" -ForegroundColor Gray
Write-Host "  Total files in configtool: $($configToolFiles.Count)" -ForegroundColor Gray

# Find shared files (files that exist in both directories)
$publishFileNames = $publishFiles | Select-Object -ExpandProperty Name
$configToolFileNames = $configToolFiles | Select-Object -ExpandProperty Name
$sharedFileNames = $publishFileNames | Where-Object { $configToolFileNames -contains $_ }
$sharedFiles = $publishFiles | Where-Object { $sharedFileNames -contains $_.Name }

Write-Host "  Shared runtime files: $($sharedFiles.Count)" -ForegroundColor Gray

# Application-specific files
$publishOnlyFiles = $publishFiles | Where-Object { $sharedFileNames -notcontains $_.Name }
$configToolOnlyFiles = $configToolFiles | Where-Object { $sharedFileNames -notcontains $_.Name }

Write-Host "  HSPrint-specific files: $($publishOnlyFiles.Count)" -ForegroundColor Gray
Write-Host "  ConfigTool-specific files: $($configToolOnlyFiles.Count)" -ForegroundColor Gray
Write-Host ""

# Helper function to create safe ID from filename
function Get-SafeId {
    param([string]$fileName)
    return $fileName -replace '[^a-zA-Z0-9]', '_'
}

# Helper function to generate component XML
function New-ComponentXml {
    param(
        [string]$componentId,
        [string]$fileId,
        [string]$sourcePath
    )
    
    $sourcePath = $sourcePath -replace '\\', '/'
    
    return @"

      <Component Id="$componentId" Guid="*">
        <File Id="$fileId" Source="$sourcePath" />
      </Component>
"@
}

# Generate Shared Runtime Files (GeneratedSharedFiles.wxs)
Write-Host "Generating shared runtime files..." -ForegroundColor Yellow
$sharedXml = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="SharedRuntimeFiles" Directory="INSTALLFOLDER">
"@

$componentIndex = 0
foreach ($file in $sharedFiles | Sort-Object Name) {
    $componentIndex++
    $safeFileName = Get-SafeId $file.Name
    $componentId = "Shared_$safeFileName"
    $fileId = "SharedFile_$componentIndex"
    $relativePath = "..\$PublishDir\$($file.Name)"
    
    $sharedXml += New-ComponentXml -componentId $componentId -fileId $fileId -sourcePath $relativePath
}

$sharedXml += @"

    </ComponentGroup>
  </Fragment>
</Wix>
"@

$sharedOutputFile = Join-Path $OutputDir "GeneratedSharedFiles.wxs"
Set-Content -Path $sharedOutputFile -Value $sharedXml -Encoding UTF8
Write-Host "  ✓ Created $sharedOutputFile with $componentIndex components" -ForegroundColor Green

# Generate HSPrint-specific files (GeneratedFiles.wxs)
Write-Host "Generating HSPrint-specific files..." -ForegroundColor Yellow
$publishXml = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="PublishedFiles" Directory="INSTALLFOLDER">
"@

$componentIndex = 0
foreach ($file in $publishOnlyFiles | Sort-Object Name) {
    $componentIndex++
    $safeFileName = Get-SafeId $file.Name
    $componentId = "File_$safeFileName"
    $fileId = "File_$componentIndex"
    $relativePath = "..\$PublishDir\$($file.Name)"
    
    $publishXml += New-ComponentXml -componentId $componentId -fileId $fileId -sourcePath $relativePath
}

$publishXml += @"

    </ComponentGroup>
  </Fragment>
</Wix>
"@

$publishOutputFile = Join-Path $OutputDir "GeneratedFiles.wxs"
Set-Content -Path $publishOutputFile -Value $publishXml -Encoding UTF8
Write-Host "  ✓ Created $publishOutputFile with $componentIndex components" -ForegroundColor Green

# Generate ConfigTool-specific files (GeneratedConfigToolFiles.wxs)
Write-Host "Generating ConfigTool-specific files..." -ForegroundColor Yellow
$configToolXml = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="ConfigToolFiles" Directory="INSTALLFOLDER">
"@

$componentIndex = 0
foreach ($file in $configToolOnlyFiles | Sort-Object Name) {
    $componentIndex++
    $safeFileName = Get-SafeId $file.Name
    $componentId = "ConfigTool_$safeFileName"
    $fileId = "ConfigToolFile_$componentIndex"
    $relativePath = "..\$ConfigToolPublishDir\$($file.Name)"
    
    $configToolXml += New-ComponentXml -componentId $componentId -fileId $fileId -sourcePath $relativePath
}

$configToolXml += @"

    </ComponentGroup>
  </Fragment>
</Wix>
"@

$configToolOutputFile = Join-Path $OutputDir "GeneratedConfigToolFiles.wxs"
Set-Content -Path $configToolOutputFile -Value $configToolXml -Encoding UTF8
Write-Host "  ✓ Created $configToolOutputFile with $componentIndex components" -ForegroundColor Green

Write-Host ""
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Green
Write-Host "✓ WiX file generation completed successfully!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  - Shared runtime files: $($sharedFiles.Count) components" -ForegroundColor White
Write-Host "  - HSPrint-specific files: $($publishOnlyFiles.Count) components" -ForegroundColor White
Write-Host "  - ConfigTool-specific files: $($configToolOnlyFiles.Count) components" -ForegroundColor White
Write-Host "  - Total components: $(($sharedFiles.Count) + ($publishOnlyFiles.Count) + ($configToolOnlyFiles.Count))" -ForegroundColor White
Write-Host ""

exit 0
