# HSPrint API Test Examples
# Run these commands using PowerShell to test the API

$baseUrl = "http://localhost:50246"

Write-Host "HSPrint API Tests" -ForegroundColor Cyan
Write-Host "=================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Health Check
Write-Host "1. Testing Health Check..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get
    Write-Host "✓ Status: $($health.status)" -ForegroundColor Green
    Write-Host "  Version: $($health.version)" -ForegroundColor Gray
    Write-Host "  PID: $($health.pid)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Health check failed: $_" -ForegroundColor Red
}
Write-Host ""

# Test 2: Get Version
Write-Host "2. Testing Version Endpoint..." -ForegroundColor Yellow
try {
    $version = Invoke-RestMethod -Uri "$baseUrl/health/version" -Method Get
    Write-Host "✓ Version: $($version.version)" -ForegroundColor Green
} catch {
    Write-Host "✗ Version check failed: $_" -ForegroundColor Red
}
Write-Host ""

# Test 3: List Printers
Write-Host "3. Testing Printer List..." -ForegroundColor Yellow
try {
 $printers = Invoke-RestMethod -Uri "$baseUrl/printer" -Method Get
    Write-Host "✓ Found $($printers.Count) printer(s):" -ForegroundColor Green
    foreach ($printer in $printers) {
        Write-Host "  - $printer" -ForegroundColor Gray
 }
} catch {
    Write-Host "✗ Printer list failed: $_" -ForegroundColor Red
}
Write-Host ""

# Test 4: Print ZPL (example)
Write-Host "4. Testing ZPL Print (example)..." -ForegroundColor Yellow
Write-Host "  Skipping actual print - uncomment to test" -ForegroundColor Gray
<#
$zplRequest = @{
    printerName = "Your Zebra Printer Name"
    zpl = "^XA^FO50,50^ADN,36,20^FDTest Print^FS^XZ"
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri "$baseUrl/print/zpl" -Method Post -Body $zplRequest -ContentType "application/json"
    Write-Host "✓ ZPL Print: $($result.message)" -ForegroundColor Green
} catch {
    Write-Host "✗ ZPL Print failed: $_" -ForegroundColor Red
}
#>
Write-Host ""

# Test 5: Print Image (example)
Write-Host "5. Testing Image Print (example)..." -ForegroundColor Yellow
Write-Host "  Skipping actual print - uncomment to test" -ForegroundColor Gray
<#
# Create a simple test image (1x1 white pixel)
$base64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg=="

$imageRequest = @{
    printerName = "Your Printer Name"
    base64Png = $base64Png
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri "$baseUrl/print/image" -Method Post -Body $imageRequest -ContentType "application/json"
    Write-Host "✓ Image Print: $($result.message)" -ForegroundColor Green
} catch {
    Write-Host "✗ Image Print failed: $_" -ForegroundColor Red
}
#>
Write-Host ""

# Test 6: Check for Updates
Write-Host "6. Testing Update Check..." -ForegroundColor Yellow
try {
    $update = Invoke-RestMethod -Uri "$baseUrl/health/update" -Method Get
    if ($update.updateAvailable) {
        Write-Host "✓ Update available: $($update.newVersion)" -ForegroundColor Yellow
    } else {
        Write-Host "✓ No updates available" -ForegroundColor Green
    }
} catch {
 Write-Host "✗ Update check failed: $_" -ForegroundColor Red
}
Write-Host ""

Write-Host "All tests completed!" -ForegroundColor Cyan
Write-Host ""
Write-Host "For interactive testing, open Swagger UI at: $baseUrl" -ForegroundColor Cyan
