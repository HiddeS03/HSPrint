# Generate WiX file list for ConfigTool files
# This script creates a WiX fragment with all ConfigTool files from the publish directory

param(
    [string]$PublishDir = "bin\Release\net8.0-windows10.0.26100.0\win-x64\configtool",
    [string]$OutputFile = "HSPrint.Installer\GeneratedConfigToolFiles.wxs"
)

Write-Host "Generating ConfigTool WiX file list from: $PublishDir"

if (-not (Test-Path $PublishDir)) {
    Write-Error "Publish directory not found: $PublishDir"
    exit 1
}

# Get all files except the ones we already include in Product.wxs
$excludeFiles = @(
    "HSPrint.ConfigTool.exe",
    "HSPrint.ConfigTool.dll",
    "HSPrint.ConfigTool.pdb",
    "HSPrint.ConfigTool.deps.json",
    "HSPrint.ConfigTool.runtimeconfig.json"
)

$files = Get-ChildItem -Path $PublishDir -File | Where-Object { 
    $excludeFiles -notcontains $_.Name 
}

Write-Host "Found $($files.Count) ConfigTool files to include"

# Start building the WiX fragment
$xml = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="ConfigToolFiles" Directory="INSTALLFOLDER">
"@

$componentIndex = 0
foreach ($file in $files) {
    $componentIndex++
    $safeFileName = $file.Name -replace '[^a-zA-Z0-9]', '_'
    $componentId = "ConfigTool_$safeFileName"
    $fileId = "ConfigToolFile_$componentIndex"
    
    # Create relative path from installer project to publish dir
    $relativePath = "..\$PublishDir\$($file.Name)"
    $relativePath = $relativePath -replace '\\', '/'
    
    $xml += @"

      <Component Id="$componentId" Guid="*">
        <File Id="$fileId" Source="$relativePath" />
      </Component>
"@
}

$xml += @"

    </ComponentGroup>
  </Fragment>
</Wix>
"@

# Write to output file
Set-Content -Path $OutputFile -Value $xml -Encoding UTF8

Write-Host "Generated ConfigTool file list: $OutputFile"
Write-Host "Included $componentIndex components"
