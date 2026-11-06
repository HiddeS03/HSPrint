<#
  HSPrint - lokaal testen van "printen via internet".

  Starten (PowerShell als administrator):
    irm https://raw.githubusercontent.com/HiddeS03/HSPrint/cloud-print/scripts/test-cloud-print.ps1 | iex

  Wat het doet:
    1. Installeert zo nodig Git en de .NET 8 SDK (via winget)
    2. Haalt de branch cloud-print op naar C:\HSPrint-test\src
    3. Maakt een nep-Zebra "Zebra Testprinter (HSPrint)": Windows stuurt de
       labels naar dit venster, dat elk label als plaatje laat zien (via Labelary)
    4. Zet de geinstalleerde HSPrint-service tijdelijk stil en start de testversie
    5. Opent Label Genie > Printers

  Stoppen: Ctrl+C. De echte HSPrint-service wordt dan weer gestart.
  Testprinter weghalen: dit script opnieuw starten met  $env:HSPRINT_CLEANUP=1
#>

$ErrorActionPreference = 'Stop'
$Root = 'C:\HSPrint-test'
$Src = Join-Path $Root 'src'
$Out = Join-Path $Root 'labels'
$Branch = 'cloud-print'
$PrinterName = 'Zebra Testprinter (HSPrint)'
$PortName = 'HSPrintTest_9109'
$ListenPort = 9109
$ServiceName = 'HSPrintService'
$PreviewUrl = 'https://hs-software-git-label-genie-editor-v2-hidde-steginks-projects.vercel.app/apps/label-genie'

function Step($text) { Write-Host "`n==> $text" -ForegroundColor Cyan }
function Ok($text) { Write-Host "    $text" -ForegroundColor Green }

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
  Write-Host 'Start PowerShell als administrator (rechtsklik > Als administrator uitvoeren) en plak de regel opnieuw.' -ForegroundColor Yellow
  return
}

if ($env:HSPRINT_CLEANUP -eq '1') {
  Step 'Testprinter verwijderen'
  Remove-Printer -Name $PrinterName -ErrorAction SilentlyContinue
  Remove-PrinterPort -Name $PortName -ErrorAction SilentlyContinue
  Ok 'Klaar. (Map C:\HSPrint-test kun je zelf weggooien.)'
  return
}

function Refresh-Path {
  $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User')
}

# 1. Tools ---------------------------------------------------------------
Step 'Git en .NET 8 SDK controleren'
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
  winget install --id Git.Git -e --silent --accept-source-agreements --accept-package-agreements | Out-Null
  Refresh-Path
}
$hasSdk8 = $false
if (Get-Command dotnet -ErrorAction SilentlyContinue) { $hasSdk8 = (dotnet --list-sdks) -match '^8\.' }
if (-not $hasSdk8) {
  Write-Host '    .NET 8 SDK installeren (paar minuten)...'
  winget install --id Microsoft.DotNet.SDK.8 -e --silent --accept-source-agreements --accept-package-agreements | Out-Null
  Refresh-Path
}
Ok "git $((git --version) -replace 'git version ','') / dotnet $(dotnet --version)"

# 2. Code ----------------------------------------------------------------
Step "Branch $Branch ophalen"
New-Item -ItemType Directory -Force -Path $Root, $Out | Out-Null
if (Test-Path (Join-Path $Src '.git')) {
  git -C $Src fetch --depth 1 origin $Branch 2>$null
  git -C $Src checkout -q -B $Branch FETCH_HEAD
} else {
  git clone -q --depth 1 -b $Branch https://github.com/HiddeS03/HSPrint.git $Src
}
Ok (git -C $Src log --oneline -1)

Step 'Bouwen'
dotnet build (Join-Path $Src 'HSPrint.csproj') -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Bouwen mislukt, zie de fouten hierboven.' }
Ok 'Gebouwd'

# 3. Test printer --------------------------------------------------------
Step "Testprinter '$PrinterName'"
if (-not (Get-PrinterDriver -Name 'Generic / Text Only' -ErrorAction SilentlyContinue)) {
  Add-PrinterDriver -Name 'Generic / Text Only'
}
if (-not (Get-PrinterPort -Name $PortName -ErrorAction SilentlyContinue)) {
  Add-PrinterPort -Name $PortName -PrinterHostAddress '127.0.0.1' -PortNumber $ListenPort
}
$existing = Get-Printer -Name $PrinterName -ErrorAction SilentlyContinue
if ($existing -and $existing.PortName -ne $PortName) { Set-Printer -Name $PrinterName -PortName $PortName }
if (-not $existing) {
  Add-Printer -Name $PrinterName -DriverName 'Generic / Text Only' -PortName $PortName
}
Ok "Labels komen in $Out"

# 4. HSPrint test version -------------------------------------------------
Step 'HSPrint testversie starten'
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
$serviceWasRunning = $service -and $service.Status -eq 'Running'
if ($serviceWasRunning) {
  Stop-Service -Name $ServiceName -Force
  Ok 'Geinstalleerde HSPrint tijdelijk gestopt'
}
Get-Process -Name 'HSPrint' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# --service: skip writing the Windows startup entry for this test build.
$exe = Get-ChildItem -Path (Join-Path $Src 'bin\Release') -Recurse -Filter 'HSPrint.exe' | Select-Object -First 1
$hsprint = Start-Process -FilePath $exe.FullName -ArgumentList '--service' -WorkingDirectory $exe.DirectoryName -PassThru
$ready = $false
for ($i = 0; $i -lt 30 -and -not $ready; $i++) {
  Start-Sleep -Milliseconds 500
  try { Invoke-RestMethod 'http://localhost:5246/health' -TimeoutSec 2 | Out-Null; $ready = $true } catch { }
}
if (-not $ready) { throw 'HSPrint testversie start niet (zie het HSPrint-venster).' }
Ok 'HSPrint draait op http://localhost:5246'

Step 'Label Genie openen'
Start-Process $PreviewUrl
Write-Host @"
    In Label Genie:
      1. Tab Printers > 'Deze computer koppelen'
      2. Printer toevoegen > Aan een andere computer > '$PrinterName'
      3. Klik Testprint. Het label verschijnt hier als plaatje.
    Ook leuk: zet hem als standaardprinter en print een bestelling, of
    gebruik je telefoon (zelfde account) met deze printer.

    Wachten op labels... (Ctrl+C om te stoppen)
"@

# 5. Fake Zebra: receive raw ZPL and show it -----------------------------
function Show-Label([string]$zpl, [int]$n) {
  $file = Join-Path $Out ("label-{0:D3}" -f $n)
  [IO.File]::WriteAllText("$file.zpl", $zpl)
  $dots = 203
  if ($zpl -match '\^PW(\d+)') { $pw = [int]$Matches[1] } else { $pw = 406 }
  if ($zpl -match '\^LL(\d+)') { $ll = [int]$Matches[1] } else { $ll = 240 }
  if ($pw -gt 900) { $dots = 300 }
  $dpmm = @{ 203 = '8dpmm'; 300 = '12dpmm' }[$dots]
  $w = [Math]::Round($pw / $dots, 2).ToString([Globalization.CultureInfo]::InvariantCulture)
  $h = [Math]::Round($ll / $dots, 2).ToString([Globalization.CultureInfo]::InvariantCulture)
  try {
    Invoke-WebRequest -Uri "http://api.labelary.com/v1/printers/$dpmm/labels/${w}x${h}/0/" -Method Post -Body $zpl `
      -ContentType 'application/x-www-form-urlencoded' -Headers @{ Accept = 'image/png' } -OutFile "$file.png" -UseBasicParsing | Out-Null
    Start-Process "$file.png"
    Ok ("Label {0} ontvangen ({1} bytes) > {2}.png" -f $n, $zpl.Length, $file)
  } catch {
    Ok ("Label {0} ontvangen ({1} bytes) > {2}.zpl (plaatje maken lukte niet: {3})" -f $n, $zpl.Length, $file, $_.Exception.Message)
  }
}

$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $ListenPort)
$count = 0
try {
  $listener.Start()
  while ($true) {
    if (-not $listener.Pending()) {
      if ($hsprint.HasExited) { Write-Host 'HSPrint testversie is gestopt.' -ForegroundColor Yellow; break }
      Start-Sleep -Milliseconds 200
      continue
    }
    $client = $listener.AcceptTcpClient()
    $stream = $client.GetStream()
    $buffer = New-Object IO.MemoryStream
    $stream.ReadTimeout = 2000
    $chunk = New-Object byte[] 65536
    try {
      while (($read = $stream.Read($chunk, 0, $chunk.Length)) -gt 0) { $buffer.Write($chunk, 0, $read) }
    } catch { }
    $client.Close()
    $data = [Text.Encoding]::UTF8.GetString($buffer.ToArray())
    if ($data.Trim().Length -gt 0) { $count++; Show-Label $data $count }
  }
} finally {
  $listener.Stop()
  if ($hsprint -and -not $hsprint.HasExited) { Stop-Process -Id $hsprint.Id -Force }
  if ($serviceWasRunning) {
    Start-Service -Name $ServiceName
    Write-Host '    Geinstalleerde HSPrint weer gestart.' -ForegroundColor Green
  }
}
