# Quick Start: Testing HSPrint Network Features

## Setup Two PCs for Testing

### PC-01 (Your Development Machine)

1. **Enable Network Mode**
   
   Edit `appsettings.json`:
   ```json
   {
     "Port": 5246,
     "Network": {
       "EnableNetworkMode": true
     }
   }
   ```

2. **Allow Firewall**
   ```powershell
   New-NetFirewallRule -DisplayName "HSPrint Network" -Direction Inbound -Protocol TCP -LocalPort 5246 -Action Allow
   ```

3. **Start HSPrint**
   ```powershell
   dotnet run
   ```

4. **Get Your IP Address**
   ```powershell
   ipconfig
   # Note your IPv4 Address, e.g., 192.168.1.100
   ```

5. **Test Local Access**
   ```powershell
   curl http://localhost:5246/network/info
   ```

### PC-02 (Another Machine on Same Network)

1. **Repeat steps 1-5 on PC-02**

2. **Note PC-02's IP address** (e.g., 192.168.1.101)

## Test Scenarios

### Test 1: Get Network Info from Both PCs

```powershell
# From PC-01, check its own info
curl http://localhost:5246/network/info

# From PC-01, check PC-02's info
curl http://192.168.1.101:5246/network/info

# From PC-02, check PC-01's info
curl http://192.168.1.100:5246/network/info
```

**Expected Result:** Both return JSON with hostname, IP, printers.

### Test 2: Direct Print (Local)

```powershell
# On PC-01, print to local printer
curl -X POST http://localhost:5246/print/zpl `
  -H "Content-Type: application/json" `
  -d '{
    "printerName": "Zebra ZD421",
    "zpl": "^XA^FO50,50^ADN,36,20^FDLocal Print Test^FS^XZ"
  }'
```

**Expected Result:** Label prints on PC-01's local printer.

### Test 3: Proxy Print (PC-01 → PC-02)

```powershell
# On PC-01, send print job to PC-02's printer
curl -X POST http://localhost:5246/network/print `
  -H "Content-Type: application/json" `
  -d '{
    "targetIp": "192.168.1.101",
    "targetPort": 5246,
    "printerName": "Zebra GK420",
    "printType": "zpl",
    "data": "^XA^FO50,50^ADN,36,20^FDRemote Print via PC-01^FS^XZ"
  }'
```

**Expected Result:** Label prints on PC-02's printer, proxied through PC-01.

### Test 4: Website Simulation (JavaScript)

Create a test HTML file:

```html
<!DOCTYPE html>
<html>
<head>
    <title>HSPrint Network Test</title>
</head>
<body>
    <h1>HSPrint Network Test</h1>
    
    <h2>1. Discover Agents</h2>
    <button onclick="discoverAgent1()">Discover PC-01 (192.168.1.100)</button>
    <button onclick="discoverAgent2()">Discover PC-02 (192.168.1.101)</button>
    <pre id="discovery-result"></pre>

    <h2>2. Print Direct</h2>
    <button onclick="printDirect()">Print to PC-01 Local Printer</button>
    <pre id="print-direct-result"></pre>

    <h2>3. Print Proxy</h2>
    <button onclick="printProxy()">Print to PC-02 via PC-01</button>
    <pre id="print-proxy-result"></pre>

    <script>
        async function discoverAgent1() {
            const result = await fetch('http://192.168.1.100:5246/network/info');
            const data = await result.json();
            document.getElementById('discovery-result').textContent = 
                'PC-01:\n' + JSON.stringify(data, null, 2);
        }

        async function discoverAgent2() {
            const result = await fetch('http://192.168.1.101:5246/network/info');
            const data = await result.json();
            document.getElementById('discovery-result').textContent = 
                'PC-02:\n' + JSON.stringify(data, null, 2);
        }

        async function printDirect() {
            const result = await fetch('http://192.168.1.100:5246/print/zpl', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    printerName: 'Zebra ZD421',
                    zpl: '^XA^FO50,50^ADN,36,20^FDDirect Print Test^FS^XZ'
                })
            });
            const data = await result.json();
            document.getElementById('print-direct-result').textContent = 
                JSON.stringify(data, null, 2);
        }

        async function printProxy() {
            const result = await fetch('http://192.168.1.100:5246/network/print', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    targetIp: '192.168.1.101',
                    targetPort: 5246,
                    printerName: 'Zebra GK420',
                    printType: 'zpl',
                    data: '^XA^FO50,50^ADN,36,20^FDProxy Print Test^FS^XZ'
                })
            });
            const data = await result.json();
            document.getElementById('print-proxy-result').textContent = 
                JSON.stringify(data, null, 2);
        }
    </script>
</body>
</html>
```

**To test:**
1. Save as `test-hsprint.html`
2. Open in browser
3. Click buttons to test each scenario
4. Check browser console for any errors

**Note:** This will only work if both HSPrint instances have `EnableNetworkMode: true` and CORS allows all origins.

## Troubleshooting

### "Connection refused" error

**Cause:** HSPrint is not listening on network interface.

**Fix:**
1. Verify `EnableNetworkMode: true` in `appsettings.json`
2. Restart HSPrint
3. Check logs: `%ProgramData%\HSPrint\logs\`
4. Look for: `"Network mode enabled - Listening on all interfaces at port 5246"`

### "No route to host" error

**Cause:** Firewall blocking connection.

**Fix:**
```powershell
# Check firewall rule exists
Get-NetFirewallRule -DisplayName "HSPrint Network"

# If not, create it:
New-NetFirewallRule -DisplayName "HSPrint Network" -Direction Inbound -Protocol TCP -LocalPort 5246 -Action Allow
```

### CORS error in browser

**Cause:** Browser blocking cross-origin request.

**Expected:** This is normal! Your production website should call HSPrint from backend, not frontend.

**For testing only:** HSPrint automatically allows all CORS when `EnableNetworkMode: true`.

### Printer not found

**Cause:** Printer name doesn't match exactly.

**Fix:**
1. Get exact printer names:
   ```powershell
   curl http://192.168.1.100:5246/printer
   ```
2. Use exact name (case-sensitive) in print request

## Success Indicators

✅ **Setup is working if:**
- `/network/info` returns valid JSON with your PC's IP and printers
- You can access `/network/info` from another PC on the network
- Direct print jobs succeed locally
- Proxy print jobs succeed to remote PCs
- Logs show: `"Network mode enabled"`, `"Successfully forwarded print job"`

❌ **Common mistakes:**
- Forgot to set `EnableNetworkMode: true`
- Didn't restart HSPrint after config change
- Firewall rule not created
- Using wrong IP address (check with `ipconfig`)
- Printer name typo or case mismatch

## Next Steps

Once testing is successful:
1. Install HSPrint on all PCs that need printing
2. Document IP addresses of all agents
3. Provide list to website team for integration
4. Configure production appsettings with proper CORS if needed
