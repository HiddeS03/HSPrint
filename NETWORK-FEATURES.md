# HSPrint Network Features

## Overview
HSPrint now supports network-based printing, allowing multiple HSPrint instances to communicate and share printers across a network.

## How It Works

### Architecture
```
┌──────────────────────┐
│   Your Website       │  Manages list of HSPrint agents per user account
│   hssoftware.nl      │
└──────────┬───────────┘
           │
           │ 1. Website discovers agents via GET /network/info
           │ 2. Website stores: UserID → [{ip, hostname, printers}]
           │ 3. Website sends print jobs directly or via proxy
           │
           ├─────────────────────┬─────────────────────┐
           │                     │                     │
           ▼                     ▼                     ▼
    ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
    │  PC-01      │      │  PC-02      │      │  PC-03      │
    │  :5246      │◄────►│  :5246      │◄────►│  :5246      │
    │             │      │             │      │             │
    │  Zebra-ZD   │      │  Zebra-GK   │      │  HP-LaserJet│
    └─────────────┘      └─────────────┘      └─────────────┘
```

### Two Printing Modes

#### Mode 1: Direct Printing (Website → HSPrint → USB Printer)
```
Website → http://192.168.1.100:5246/print/zpl → HSPrint on PC-01 → USB Printer
```

#### Mode 2: Proxy Printing (Website → HSPrint A → HSPrint B → USB Printer)
```
Website → http://192.168.1.100:5246/network/print → HSPrint on PC-01 
                                                     ↓
                              http://192.168.1.101:5246/print/zpl → HSPrint on PC-02 
                                                                      ↓
                                                                   USB Printer
```

## Configuration

### Enable Network Mode

Edit `appsettings.json`:

```json
{
  "Port": 5246,
  "Network": {
    "EnableNetworkMode": true
  }
}
```

**Important:** When `EnableNetworkMode` is `true`:
- HSPrint listens on **all network interfaces** (0.0.0.0:5246)
- Other PCs on the network can connect
- CORS allows all origins

When `false` (default):
- HSPrint listens only on **localhost** (127.0.0.1:5246)
- Only local applications can connect
- CORS restricted to configured origins

### Firewall Configuration

**Windows Firewall must allow inbound connections on port 5246:**

```powershell
# Allow HSPrint through Windows Firewall
New-NetFirewallRule -DisplayName "HSPrint Network Mode" `
  -Direction Inbound `
  -Protocol TCP `
  -LocalPort 5246 `
  -Action Allow
```

## API Endpoints

### 1. Get Network Info
**Endpoint:** `GET /network/info`

Returns information about this HSPrint instance.

**Response:**
```json
{
  "hostname": "PC-01",
  "ipAddress": "192.168.1.100",
  "port": 5246,
  "version": "1.2.0",
  "printers": [
    { "name": "Zebra ZD421", "isDefault": false },
    { "name": "HP LaserJet", "isDefault": true }
  ],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### 2. Get Remote Node Info
**Endpoint:** `GET /network/remote/info?ip=192.168.1.101&port=5246`

Query another HSPrint instance for its info.

**Response:** Same as `/network/info`

### 3. Forward Print Job to Remote Node
**Endpoint:** `POST /network/print`

Forward a print job to another HSPrint instance on the network.

**Request Body:**
```json
{
  "targetIp": "192.168.1.101",
  "targetPort": 5246,
  "printerName": "Zebra ZD421",
  "printType": "zpl",
  "data": "^XA^FO50,50^ADN,36,20^FDHello World^FS^XZ"
}
```

**Parameters:**
- `targetIp`: IP address of the target HSPrint instance
- `targetPort`: Port of the target HSPrint instance (default: 5246)
- `printerName`: Name of the printer on the target PC
- `printType`: Type of print job: `"zpl"`, `"image"`, or `"pdf"`
- `data`: Print data (ZPL string, Base64 PNG, or Base64 PDF)

**Response:**
```json
{
  "success": true,
  "message": "Print job successfully forwarded to 192.168.1.101:5246",
  "error": null
}
```

### 4. Existing Print Endpoints (Still Available)

All existing endpoints still work for local printing:

- `POST /print/zpl` - Print ZPL locally
- `POST /print/image` - Print image locally
- `POST /print/pdf` - Print PDF locally
- `GET /printer` - List local printers

## Usage Examples

### Example 1: Discover Agent

```javascript
// JavaScript fetch from website
const response = await fetch('http://192.168.1.100:5246/network/info');
const agentInfo = await response.json();

console.log(agentInfo);
// {
//   hostname: "Office-PC-01",
//   ipAddress: "192.168.1.100",
//   port: 5246,
//   printers: ["Zebra ZD421", "HP LaserJet"],
//   ...
// }

// Store in database: UserID → Agent list
```

### Example 2: Direct Print (Website → HSPrint → Printer)

```javascript
// Print directly to PC-01's local printer
const response = await fetch('http://192.168.1.100:5246/print/zpl', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    printerName: 'Zebra ZD421',
    zpl: '^XA^FO50,50^ADN,36,20^FDLabel Text^FS^XZ'
  })
});
```

### Example 3: Proxy Print (Website → PC-01 → PC-02 → Printer)

```javascript
// Send print job to PC-01, which forwards to PC-02
const response = await fetch('http://192.168.1.100:5246/network/print', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    targetIp: '192.168.1.101',
    targetPort: 5246,
    printerName: 'Zebra GK420',
    printType: 'zpl',
    data: '^XA^FO50,50^ADN,36,20^FDRemote Print^FS^XZ'
  })
});
```

## Security Considerations

1. **Network Mode Security:**
   - When `EnableNetworkMode: true`, HSPrint accepts connections from any IP
   - Ensure HSPrint is only used on trusted networks
   - Consider implementing API key authentication in future versions

2. **Firewall:**
   - Only enable network mode and open port 5246 when needed
   - Use Windows Firewall to restrict access to specific IP ranges if needed

3. **HTTPS:**
   - Currently uses HTTP for simplicity
   - For production across untrusted networks, consider adding HTTPS support

## Troubleshooting

### Cannot connect from another PC

1. **Check Network Mode:**
   - Ensure `"EnableNetworkMode": true` in `appsettings.json`
   - Restart HSPrint service after changing config

2. **Check Firewall:**
   ```powershell
   # Test if port is open
   Test-NetConnection -ComputerName 192.168.1.100 -Port 5246
   ```

3. **Check if HSPrint is listening:**
   ```powershell
   # Run on the HSPrint host PC
   netstat -an | findstr 5246
   # Should show: TCP    0.0.0.0:5246    0.0.0.0:0    LISTENING
   ```

4. **Check IP Address:**
   - Use `ipconfig` to verify the correct IP address
   - Ensure both PCs are on the same network/subnet

### Print job forwarding fails

1. **Verify target HSPrint is running:**
   ```
   http://192.168.1.101:5246/network/info
   ```

2. **Check logs:**
   - Look in `%ProgramData%\HSPrint\logs\`
   - Check for connection errors or timeouts

3. **Verify printer name matches exactly:**
   - Printer names are case-sensitive
   - Use `/printer` endpoint to get exact names

## Version History

### v1.2.0 - Network Features
- Added `/network/info` endpoint
- Added `/network/print` proxy endpoint
- Added `EnableNetworkMode` configuration
- Added support for listening on all network interfaces
- Added CORS support for network mode
