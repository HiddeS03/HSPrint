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
    
    $xmlContent = "`n      <Component Id=`"$componentId`" Guid=`"*`">`n"
    $xmlContent += "        <File Id=`"$fileId`" Source=`"$sourcePath`" />`n"
    $xmlContent += "      </Component>"
    
    return $xmlContent
}

# Generate Shared Runtime Files (GeneratedSharedFiles.wxs)
Write-Host "Generating shared runtime files..." -ForegroundColor Yellow
$sharedXml = '<?xml version="1.0" encoding="UTF-8"?>' + "`n"
$sharedXml += '<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">' + "`n"
$sharedXml += '  <Fragment>' + "`n"
$sharedXml += '    <ComponentGroup Id="SharedRuntimeFiles" Directory="INSTALLFOLDER">'

$componentIndex = 0
foreach ($file in $sharedFiles | Sort-Object Name) {
    $componentIndex++
    $safeFileName = Get-SafeId $file.Name
    $componentId = "Shared_$safeFileName"
    $fileId = "SharedFile_$componentIndex"
    $relativePath = "..\$PublishDir\$($file.Name)"
    
    $sharedXml += New-ComponentXml -componentId $componentId -fileId $fileId -sourcePath $relativePath
}

$sharedXml += "`n    </ComponentGroup>`n"
$sharedXml += '  </Fragment>' + "`n"
$sharedXml += '</Wix>'

$sharedOutputFile = Join-Path $OutputDir "GeneratedSharedFiles.wxs"
[System.IO.File]::WriteAllText($sharedOutputFile, $sharedXml, [System.Text.UTF8Encoding]::new($false))
Write-Host "  Created $sharedOutputFile with $componentIndex components" -ForegroundColor Green

# Generate HSPrint-specific files (GeneratedFiles.wxs)
Write-Host "Generating HSPrint-specific files..." -ForegroundColor Yellow
$publishXml = '<?xml version="1.0" encoding="UTF-8"?>' + "`n"
$publishXml += '<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">' + "`n"
$publishXml += '  <Fragment>' + "`n"
$publishXml += '    <ComponentGroup Id="PublishedFiles" Directory="INSTALLFOLDER">'

$componentIndex = 0
foreach ($file in $publishOnlyFiles | Sort-Object Name) {
    $componentIndex++
    $safeFileName = Get-SafeId $file.Name
    $componentId = "File_$safeFileName"
    $fileId = "File_$componentIndex"
    $relativePath = "..\$PublishDir\$($file.Name)"
    
    $publishXml += New-ComponentXml -componentId $componentId -fileId $fileId -sourcePath $relativePath
}

$publishXml += "`n    </ComponentGroup>`n"
$publishXml += '  </Fragment>' + "`n"
$publishXml += '</Wix>'

$publishOutputFile = Join-Path $OutputDir "GeneratedFiles.wxs"
[System.IO.File]::WriteAllText($publishOutputFile, $publishXml, [System.Text.UTF8Encoding]::new($false))
Write-Host "  Created $publishOutputFile with $componentIndex components" -ForegroundColor Green

# Generate ConfigTool-specific files (GeneratedConfigToolFiles.wxs)
Write-Host "Generating ConfigTool-specific files..." -ForegroundColor Yellow
$configToolXml = '<?xml version="1.0" encoding="UTF-8"?>' + "`n"
$configToolXml += '<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">' + "`n"
$configToolXml += '  <Fragment>' + "`n"
$configToolXml += '    <ComponentGroup Id="ConfigToolFiles" Directory="INSTALLFOLDER">'

$componentIndex = 0
foreach ($file in $configToolOnlyFiles | Sort-Object Name) {
    $componentIndex++
    $safeFileName = Get-SafeId $file.Name
    $componentId = "ConfigTool_$safeFileName"
    $fileId = "ConfigToolFile_$componentIndex"
    $relativePath = "..\$ConfigToolPublishDir\$($file.Name)"
    
    $configToolXml += New-ComponentXml -componentId $componentId -fileId $fileId -sourcePath $relativePath
}

$configToolXml += "`n    </ComponentGroup>`n"
$configToolXml += '  </Fragment>' + "`n"
$configToolXml += '</Wix>'

$configToolOutputFile = Join-Path $OutputDir "GeneratedConfigToolFiles.wxs"
[System.IO.File]::WriteAllText($configToolOutputFile, $configToolXml, [System.Text.UTF8Encoding]::new($false))
Write-Host "  Created $configToolOutputFile with $componentIndex components" -ForegroundColor Green

Write-Host ""
Write-Host "WiX file generation completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  - Shared runtime files: $($sharedFiles.Count) components" -ForegroundColor White
Write-Host "  - HSPrint-specific files: $($publishOnlyFiles.Count) components" -ForegroundColor White
Write-Host "  - ConfigTool-specific files: $($configToolOnlyFiles.Count) components" -ForegroundColor White
Write-Host "  - Total components: $(($sharedFiles.Count) + ($publishOnlyFiles.Count) + ($configToolOnlyFiles.Count))" -ForegroundColor White
Write-Host ""

exit 0
