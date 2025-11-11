# Generate WiX file list for all published files
# This script creates a WiX fragment with all files from the publish directory

param(
    [string]$PublishDir = "bin\Release\net8.0-windows10.0.26100.0\win-x64\publish",
    [string]$OutputFile = "HSPrint.Installer\GeneratedFiles.wxs"
)

Write-Host "Generating WiX file list from: $PublishDir"

if (-not (Test-Path $PublishDir)) {
 Write-Error "Publish directory not found: $PublishDir"
    exit 1
}

# Get all files except the ones we already include in Product.wxs
$excludeFiles = @(
    "HSPrint.exe",
    "appsettings.json",
    "appsettings.Development.json",
    "version.txt",
    "HSPrint.dll",
    "HSPrint.pdb",
    "HSPrint.deps.json",
    "HSPrint.runtimeconfig.json"
)

$files = Get-ChildItem -Path $PublishDir -File | Where-Object { 
    $excludeFiles -notcontains $_.Name 
}

Write-Host "Found $($files.Count) files to include"

# Start building the WiX fragment
$xml = @"
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Fragment>
    <ComponentGroup Id="PublishedFiles" Directory="INSTALLFOLDER">
"@

$componentIndex = 0
foreach ($file in $files) {
    $componentIndex++
    $safeFileName = $file.Name -replace '[^a-zA-Z0-9]', '_'
    $componentId = "File_$safeFileName"
    $fileId = "File_$componentIndex"
    
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

Write-Host "Generated file list: $OutputFile"
Write-Host "Included $componentIndex components"
