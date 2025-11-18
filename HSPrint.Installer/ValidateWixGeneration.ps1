# Validation script for WiX file generation
# This script validates the generated WiX files for common issues

param(
    [string]$SharedFilesPath = "HSPrint.Installer\GeneratedSharedFiles.wxs",
    [string]$PublishedFilesPath = "HSPrint.Installer\GeneratedFiles.wxs",
    [string]$ConfigToolFilesPath = "HSPrint.Installer\GeneratedConfigToolFiles.wxs"
)

$ErrorActionPreference = "Stop"
$validationErrors = @()
$validationWarnings = @()

Write-Host "â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•" -ForegroundColor Cyan
Write-Host "  WiX File Generation Validation" -ForegroundColor Cyan
Write-Host "â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•" -ForegroundColor Cyan
Write-Host ""

# Function to extract component IDs from a WiX file
function Get-ComponentIds {
    param([string]$filePath)
    
    if (-not (Test-Path $filePath)) {
        return @()
    }
    
    try {
        [xml]$xml = Get-Content $filePath
        $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $ns.AddNamespace("wix", "http://wixtoolset.org/schemas/v4/wxs")
        $components = $xml.SelectNodes("//wix:Component[@Id]", $ns)
        return $components | ForEach-Object { $_.GetAttribute("Id") }
    }
    catch {
        Write-Host "Error parsing $filePath`: $_" -ForegroundColor Red
        return @()
    }
}

# Function to extract file IDs from a WiX file
function Get-FileIds {
    param([string]$filePath)
    
    if (-not (Test-Path $filePath)) {
        return @()
    }
    
    try {
        [xml]$xml = Get-Content $filePath
        $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $ns.AddNamespace("wix", "http://wixtoolset.org/schemas/v4/wxs")
        $files = $xml.SelectNodes("//wix:File[@Id]", $ns)
        return $files | ForEach-Object { $_.GetAttribute("Id") }
    }
    catch {
        return @()
    }
}

# Function to extract source paths from a WiX file
function Get-SourcePaths {
    param([string]$filePath)
    
    if (-not (Test-Path $filePath)) {
        return @()
    }
    
    try {
        [xml]$xml = Get-Content $filePath
        $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
        $ns.AddNamespace("wix", "http://wixtoolset.org/schemas/v4/wxs")
        $files = $xml.SelectNodes("//wix:File[@Source]", $ns)
        return $files | ForEach-Object { 
            [PSCustomObject]@{
                FileId   = $_.GetAttribute("Id")
                Source   = $_.GetAttribute("Source")
                FileName = Split-Path $_.GetAttribute("Source") -Leaf
            }
        }
    }
    catch {
        return @()
    }
}

Write-Host "Step 1: Checking file existence..." -ForegroundColor Yellow
$filesToCheck = @($SharedFilesPath, $PublishedFilesPath, $ConfigToolFilesPath)
$allFilesExist = $true
foreach ($file in $filesToCheck) {
    if (Test-Path $file) {
        Write-Host "  ✓ $file exists" -ForegroundColor Green
    } else {
        Write-Host "  âœ— $file not found" -ForegroundColor Red
        $validationErrors += "$file not found"
        $allFilesExist = $false
    }
}

if (-not $allFilesExist) {
    Write-Host ""
    Write-Host "Some files are missing. Cannot continue validation." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Validating XML structure..." -ForegroundColor Yellow
foreach ($file in $filesToCheck) {
    try {
        [xml]$xml = Get-Content $file
        Write-Host "  ✓ $file is valid XML" -ForegroundColor Green
    }
    catch {
        Write-Host "  âœ— $file has invalid XML: $_" -ForegroundColor Red
        $validationErrors += "$file has invalid XML: $_"
    }
}

Write-Host ""
Write-Host "Step 3: Checking for duplicate component IDs..." -ForegroundColor Yellow
$allComponentIds = @()
$componentIdFiles = @{}

foreach ($file in $filesToCheck) {
    $componentIds = Get-ComponentIds $file
    foreach ($id in $componentIds) {
        $allComponentIds += $id
        if (-not $componentIdFiles.ContainsKey($id)) {
            $componentIdFiles[$id] = @()
        }
        $componentIdFiles[$id] += (Split-Path $file -Leaf)
    }
}

$duplicateComponents = $componentIdFiles.GetEnumerator() | Where-Object { $_.Value.Count -gt 1 }
if ($duplicateComponents) {
    foreach ($dup in $duplicateComponents) {
        $files = $dup.Value -join ", "
        Write-Host "  âœ— Duplicate component ID '$($dup.Key)' found in: $files" -ForegroundColor Red
        $validationErrors += "Duplicate component ID: $($dup.Key)"
    }
}
else {
    Write-Host "  ✓ No duplicate component IDs found" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 4: Checking for duplicate file IDs..." -ForegroundColor Yellow
$allFileIds = @()
$fileIdFiles = @{}

foreach ($file in $filesToCheck) {
    $fileIds = Get-FileIds $file
    foreach ($id in $fileIds) {
        $allFileIds += $id
        if (-not $fileIdFiles.ContainsKey($id)) {
            $fileIdFiles[$id] = @()
        }
        $fileIdFiles[$id] += (Split-Path $file -Leaf)
    }
}

$duplicateFileIds = $fileIdFiles.GetEnumerator() | Where-Object { $_.Value.Count -gt 1 }
if ($duplicateFileIds) {
    foreach ($dup in $duplicateFileIds) {
        $files = $dup.Value -join ", "
        Write-Host "  âœ— Duplicate file ID '$($dup.Key)' found in: $files" -ForegroundColor Red
        $validationErrors += "Duplicate file ID: $($dup.Key)"
    }
} else {
    Write-Host "  ✓ No duplicate file IDs found" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 5: Checking for duplicate file names across groups..." -ForegroundColor Yellow
$sharedFiles = Get-SourcePaths $SharedFilesPath
$publishedFiles = Get-SourcePaths $PublishedFilesPath
$configToolFiles = Get-SourcePaths $ConfigToolFilesPath

$sharedFileNames = $sharedFiles | Select-Object -ExpandProperty FileName
$publishedFileNames = $publishedFiles | Select-Object -ExpandProperty FileName
$configToolFileNames = $configToolFiles | Select-Object -ExpandProperty FileName

# Check if any published files overlap with shared files
$publishedOverlap = $publishedFileNames | Where-Object { $sharedFileNames -contains $_ }
if ($publishedOverlap) {
    foreach ($file in $publishedOverlap) {
        Write-Host "  âš  File '$file' exists in both PublishedFiles and SharedRuntimeFiles" -ForegroundColor Yellow
        $validationWarnings += "File overlap: $file in PublishedFiles and SharedRuntimeFiles"
    }
}

# Check if any config tool files overlap with shared files
$configToolOverlap = $configToolFileNames | Where-Object { $sharedFileNames -contains $_ }
if ($configToolOverlap) {
    foreach ($file in $configToolOverlap) {
        Write-Host "  âš  File '$file' exists in both ConfigToolFiles and SharedRuntimeFiles" -ForegroundColor Yellow
        $validationWarnings += "File overlap: $file in ConfigToolFiles and SharedRuntimeFiles"
    }
}

if (-not $publishedOverlap -and -not $configToolOverlap) {
    Write-Host "  ✓ No file name overlaps between component groups" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 6: Checking for problematic files from issue..." -ForegroundColor Yellow
$problematicFiles = @('clretwrc.dll', 'clrgc.dll', 'clrjit.dll', 'coreclr.dll', 'createdump.exe')
$problematicFilesOk = $true

foreach ($file in $problematicFiles) {
    $locations = @()
    if ($sharedFileNames -contains $file) { $locations += "SharedRuntimeFiles" }
    if ($publishedFileNames -contains $file) { $locations += "PublishedFiles" }
    if ($configToolFileNames -contains $file) { $locations += "ConfigToolFiles" }
    
    if ($locations.Count -eq 0) {
        Write-Host "  âš  '$file' not found in any component group" -ForegroundColor Yellow
        $validationWarnings += "$file not found in any component group"
    } elseif ($locations.Count -eq 1) {
        Write-Host "  ✓ '$file' only in $($locations[0])" -ForegroundColor Green
    } else {
        Write-Host "  âœ— '$file' found in multiple locations: $($locations -join ', ')" -ForegroundColor Red
        $validationErrors += "$file found in multiple locations"
        $problematicFilesOk = $false
    }
}

Write-Host ""
Write-Host "â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•" -ForegroundColor Cyan
Write-Host "  Validation Summary" -ForegroundColor Cyan
Write-Host "â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•" -ForegroundColor Cyan
Write-Host ""
Write-Host "Statistics:" -ForegroundColor White
Write-Host "  - Shared runtime files: $($sharedFiles.Count) components" -ForegroundColor Gray
Write-Host "  - HSPrint-specific files: $($publishedFiles.Count) components" -ForegroundColor Gray
Write-Host "  - ConfigTool-specific files: $($configToolFiles.Count) components" -ForegroundColor Gray
Write-Host "  - Total components: $(($sharedFiles.Count) + ($publishedFiles.Count) + ($configToolFiles.Count))" -ForegroundColor Gray
Write-Host ""

if ($validationErrors.Count -eq 0) {
    Write-Host "[OK] Validation PASSED" -ForegroundColor Green -BackgroundColor Black
    if ($validationWarnings.Count -gt 0) {
        Write-Host ""
        Write-Host "Warnings ($($validationWarnings.Count)):" -ForegroundColor Yellow
        foreach ($warning in $validationWarnings) {
            Write-Host "  âš  $warning" -ForegroundColor Yellow
        }
    }
    exit 0
} else {
    Write-Host "[FAILED] Validation FAILED" -ForegroundColor Red -BackgroundColor Black
    Write-Host ""
    Write-Host "Errors ($($validationErrors.Count)):" -ForegroundColor Red
    foreach ($validationError in $validationErrors) {
        Write-Host "  X $validationError" -ForegroundColor Red
    }
    if ($validationWarnings.Count -gt 0) {
        Write-Host ""
        Write-Host "Warnings ($($validationWarnings.Count)):" -ForegroundColor Yellow
        foreach ($warning in $validationWarnings) {
            Write-Host "  ! $warning" -ForegroundColor Yellow
        }
    }
    exit 1
}
