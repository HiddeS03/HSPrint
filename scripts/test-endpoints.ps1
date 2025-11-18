param(
    [string]$baseUrl = "http://localhost:50246",
    [string]$printerName = "YourPrinterNameHere",
    [string]$ip = "192.168.1.100",
    [int]$port = 9100
)

Write-Host "Testing HSPrint endpoints at: $baseUrl" -ForegroundColor Cyan

# Health
Write-Host "\nGET /health" -ForegroundColor Yellow
Invoke-RestMethod -Method Get -Uri "$baseUrl/health" | ConvertTo-Json -Depth 4 | Write-Host

# Version
Write-Host "\nGET /health/version" -ForegroundColor Yellow
Invoke-RestMethod -Method Get -Uri "$baseUrl/health/version" | ConvertTo-Json -Depth 4 | Write-Host

# Printers
Write-Host "\nGET /printer" -ForegroundColor Yellow
Invoke-RestMethod -Method Get -Uri "$baseUrl/printer" | ConvertTo-Json -Depth 4 | Write-Host

# Print ZPL
Write-Host "\nPOST /print/zpl" -ForegroundColor Yellow
$zpl = '^XA^FO20,20^A0N,30,30^FDTest^FS^XZ'
$body = @{ printerName = $printerName; zpl = $zpl } | ConvertTo-Json
try {
    Invoke-RestMethod -Method Post -Uri "$baseUrl/print/zpl" -ContentType 'application/json' -Body $body | ConvertTo-Json -Depth 4 | Write-Host
}
catch {
    Write-Host "Error posting /print/zpl: $_" -ForegroundColor Red
}

# Print ZPL via TCP
Write-Host "\nPOST /print/zpl/tcp" -ForegroundColor Yellow
$body2 = @{ ip = $ip; port = $port; zpl = $zpl } | ConvertTo-Json
try {
    Invoke-RestMethod -Method Post -Uri "$baseUrl/print/zpl/tcp" -ContentType 'application/json' -Body $body2 | ConvertTo-Json -Depth 4 | Write-Host
}
catch {
    Write-Host "Error posting /print/zpl/tcp: $_" -ForegroundColor Red
}

Write-Host "\nDone." -ForegroundColor Green
