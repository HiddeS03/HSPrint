# Website Integration Prompt for HSPrint Network Features

## Context
We have successfully implemented network-based printing in HSPrint. Each HSPrint agent can now:
1. Expose its network information (IP, hostname, available printers)
2. Accept print jobs directly from the website
3. Proxy/forward print jobs to other HSPrint agents on the network

## Your Task
Integrate HSPrint network capabilities into the website to enable multi-location printing for users.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Your Website (hssoftware.nl)             │
│                                                             │
│  1. User Management: Each user has a list of HSPrint agents │
│  2. Agent Discovery: Detect and register HSPrint instances  │
│  3. Printer Selection: Show all printers across all agents  │
│  4. Print Routing: Send jobs to correct agent/printer       │
└─────────────────────────────────────────────────────────────┘
                    │           │           │
                    ▼           ▼           ▼
            ┌───────────┐ ┌───────────┐ ┌───────────┐
            │ PC-01     │ │ PC-02     │ │ PC-03     │
            │ :5246     │ │ :5246     │ │ :5246     │
            │           │ │           │ │           │
            │ Zebra-ZD  │ │ Zebra-GK  │ │ HP-Laser  │
            └───────────┘ └───────────┘ └───────────┘
```

---

## Database Schema Changes

### 1. Create `HSPrintAgents` Table

```sql
CREATE TABLE HSPrintAgents (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    Hostname NVARCHAR(255) NOT NULL,
    IpAddress NVARCHAR(50) NOT NULL,
    Port INT NOT NULL DEFAULT 5246,
    Version NVARCHAR(50),
    IsActive BIT NOT NULL DEFAULT 1,
    LastSeen DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT UQ_UserAgent UNIQUE (UserId, IpAddress, Port)
);

CREATE INDEX IX_HSPrintAgents_UserId ON HSPrintAgents(UserId);
CREATE INDEX IX_HSPrintAgents_LastSeen ON HSPrintAgents(LastSeen);
```

### 2. Create `HSPrintAgentPrinters` Table

```sql
CREATE TABLE HSPrintAgentPrinters (
    Id INT PRIMARY KEY IDENTITY(1,1),
    AgentId INT NOT NULL,
    PrinterName NVARCHAR(255) NOT NULL,
    IsDefault BIT NOT NULL DEFAULT 0,
    LastSeen DATETIME2 NOT NULL,
    
    FOREIGN KEY (AgentId) REFERENCES HSPrintAgents(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_AgentPrinter UNIQUE (AgentId, PrinterName)
);

CREATE INDEX IX_HSPrintAgentPrinters_AgentId ON HSPrintAgentPrinters(AgentId);
```

---

## API Endpoints to Implement

### 1. Register/Update Agent

**Endpoint:** `POST /api/hsprint/agents/register`

**Purpose:** Users register their HSPrint instances with the website.

**Request Body:**
```json
{
  "ipAddress": "192.168.1.100",
  "port": 5246
}
```

**Implementation Steps:**
1. Call `http://{ipAddress}:{port}/network/info` to verify agent exists
2. Parse response to get hostname, version, printers
3. Insert/update agent in database
4. Insert/update printers for this agent
5. Return success with agent details

**Response:**
```json
{
  "success": true,
  "agent": {
    "id": 123,
    "hostname": "Office-PC-01",
    "ipAddress": "192.168.1.100",
    "port": 5246,
    "version": "1.2.0",
    "printers": [
      { "name": "Zebra ZD421", "isDefault": false },
      { "name": "HP LaserJet", "isDefault": true }
    ],
    "lastSeen": "2024-01-15T10:30:00Z"
  }
}
```

---

### 2. Get User's Agents

**Endpoint:** `GET /api/hsprint/agents`

**Purpose:** Get all HSPrint agents for the logged-in user.

**Response:**
```json
{
  "agents": [
    {
      "id": 123,
      "hostname": "Office-PC-01",
      "ipAddress": "192.168.1.100",
      "port": 5246,
      "version": "1.2.0",
      "isActive": true,
      "lastSeen": "2024-01-15T10:30:00Z",
      "printers": [
        { "name": "Zebra ZD421", "isDefault": false },
        { "name": "HP LaserJet", "isDefault": true }
      ]
    },
    {
      "id": 124,
      "hostname": "Warehouse-PC",
      "ipAddress": "192.168.1.101",
      "port": 5246,
      "version": "1.2.0",
      "isActive": true,
      "lastSeen": "2024-01-15T10:25:00Z",
      "printers": [
        { "name": "Zebra GK420", "isDefault": false }
      ]
    }
  ]
}
```

---

### 3. Get All Printers

**Endpoint:** `GET /api/hsprint/printers`

**Purpose:** Get flat list of all printers across all user's agents.

**Response:**
```json
{
  "printers": [
    {
      "id": "123_Zebra ZD421",
      "agentId": 123,
      "agentHostname": "Office-PC-01",
      "agentIp": "192.168.1.100",
      "agentPort": 5246,
      "printerName": "Zebra ZD421",
      "displayName": "Office-PC-01 - Zebra ZD421",
      "isDefault": false
    },
    {
      "id": "123_HP LaserJet",
      "agentId": 123,
      "agentHostname": "Office-PC-01",
      "agentIp": "192.168.1.100",
      "agentPort": 5246,
      "printerName": "HP LaserJet",
      "displayName": "Office-PC-01 - HP LaserJet",
      "isDefault": true
    },
    {
      "id": "124_Zebra GK420",
      "agentId": 124,
      "agentHostname": "Warehouse-PC",
      "agentIp": "192.168.1.101",
      "agentPort": 5246,
      "printerName": "Zebra GK420",
      "displayName": "Warehouse-PC - Zebra GK420",
      "isDefault": false
    }
  ]
}
```

---

### 4. Refresh Agent

**Endpoint:** `POST /api/hsprint/agents/{agentId}/refresh`

**Purpose:** Poll agent for updated info and printers.

**Implementation:**
1. Get agent from database
2. Call `http://{ipAddress}:{port}/network/info`
3. Update agent and printers in database
4. Return updated info

---

### 5. Delete Agent

**Endpoint:** `DELETE /api/hsprint/agents/{agentId}`

**Purpose:** Remove agent from user's account.

---

### 6. Print Job

**Endpoint:** `POST /api/hsprint/print`

**Purpose:** Send print job to specified agent/printer.

**Request Body:**
```json
{
  "agentId": 123,
  "printerName": "Zebra ZD421",
  "printType": "zpl",
  "data": "^XA^FO50,50^ADN,36,20^FDHello World^FS^XZ"
}
```

**OR (using printer ID from GET /api/hsprint/printers):**
```json
{
  "printerId": "123_Zebra ZD421",
  "printType": "zpl",
  "data": "^XA^FO50,50^ADN,36,20^FDHello World^FS^XZ"
}
```

**Implementation:**
1. Parse agentId and printerName (from ID or direct parameters)
2. Get agent details from database
3. **Make backend HTTP call** to HSPrint agent:
   ```
   POST http://{agentIp}:{agentPort}/print/zpl
   {
     "printerName": "Zebra ZD421",
     "zpl": "^XA^FO50,50^ADN,36,20^FDHello World^FS^XZ"
   }
   ```
4. Return result to user

**Response:**
```json
{
  "success": true,
  "message": "Print job sent successfully to Office-PC-01 - Zebra ZD421"
}
```

---

## Frontend UI Changes

### 1. Agent Management Page

**Path:** `/account/hsprint-agents`

**Features:**
- List all registered agents with hostname, IP, status
- "Add Agent" button that prompts for IP:Port
- "Refresh" button per agent to update printer list
- "Delete" button per agent
- Show last seen timestamp
- Show online/offline status

**Mockup:**
```
┌────────────────────────────────────────────────────────────┐
│  HSPrint Agents                                   [+ Add]  │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ● Office-PC-01 (192.168.1.100:5246)       [Refresh] [×]  │
│    Online - Last seen: 2 minutes ago                       │
│    Printers: Zebra ZD421, HP LaserJet                      │
│                                                            │
│  ● Warehouse-PC (192.168.1.101:5246)       [Refresh] [×]  │
│    Online - Last seen: 5 minutes ago                       │
│    Printers: Zebra GK420                                   │
│                                                            │
│  ○ Remote-Office (192.168.2.50:5246)       [Refresh] [×]  │
│    Offline - Last seen: 2 hours ago                        │
│    Printers: HP LaserJet Pro                               │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### 2. Add Agent Modal

```
┌─────────────────────────────────────┐
│  Add HSPrint Agent                 │
├─────────────────────────────────────┤
│                                     │
│  IP Address: [192.168.1.100]       │
│  Port:       [5246        ]        │
│                                     │
│  [Cancel]            [Add Agent]   │
│                                     │
└─────────────────────────────────────┘
```

### 3. Printer Selection Dropdown

When user needs to print, show dropdown grouped by agent:

```
┌──────────────────────────────────────┐
│ Select Printer              ▼        │
├──────────────────────────────────────┤
│ Office-PC-01                         │
│   • Zebra ZD421                      │
│   • HP LaserJet                      │
│                                      │
│ Warehouse-PC                         │
│   • Zebra GK420                      │
│                                      │
│ Remote-Office (Offline)              │
│   • HP LaserJet Pro (unavailable)    │
└──────────────────────────────────────┘
```

---

## Security Considerations

### 1. CORS in Browser

**Problem:** Modern browsers block cross-origin HTTP requests from frontend JavaScript.

**Solution:** Make all HSPrint API calls from your **backend**, not frontend.

```javascript
// ❌ DON'T DO THIS (browser will block cross-origin request)
fetch('http://192.168.1.100:5246/print/zpl', { ... });

// ✅ DO THIS INSTEAD (call your backend API)
fetch('https://hssoftware.nl/api/hsprint/print', {
  method: 'POST',
  body: JSON.stringify({
    agentId: 123,
    printerName: 'Zebra ZD421',
    printType: 'zpl',
    data: zplString
  })
});

// Your backend then calls:
// POST http://192.168.1.100:5246/print/zpl
```

### 2. IP Address Validation

Validate IP addresses to prevent SSRF attacks:
- Only allow private IP ranges: 10.x.x.x, 172.16-31.x.x, 192.168.x.x
- Block localhost (127.x.x.x) and link-local (169.254.x.x)
- Verify agent responds with valid HSPrint info before storing

### 3. Rate Limiting

Implement rate limiting on agent registration/refresh to prevent abuse.

---

## Background Jobs

### 1. Agent Health Check

**Frequency:** Every 5 minutes

**Purpose:** Check if agents are still online and update status.

```csharp
// Pseudocode
foreach (var agent in GetAllAgents())
{
    try
    {
        var info = await HttpClient.GetAsync($"http://{agent.IpAddress}:{agent.Port}/network/info");
        if (info.IsSuccess)
        {
            agent.IsActive = true;
            agent.LastSeen = DateTime.UtcNow;
            UpdatePrinters(agent, info.Printers);
        }
    }
    catch
    {
        agent.IsActive = false;
    }
    SaveAgent(agent);
}
```

### 2. Stale Agent Cleanup

**Frequency:** Daily

**Purpose:** Mark agents as inactive if not seen in 24 hours.

---

## Testing Checklist

### Backend Tests
- [ ] Can register new agent
- [ ] Can update existing agent
- [ ] Can delete agent
- [ ] Can get all agents for user
- [ ] Can get all printers for user
- [ ] Can send print job to agent
- [ ] Handles offline agents gracefully
- [ ] Validates IP addresses properly
- [ ] Rate limiting works

### Frontend Tests
- [ ] Agent list displays correctly
- [ ] Can add new agent
- [ ] Can refresh agent
- [ ] Can delete agent
- [ ] Printer dropdown shows all printers
- [ ] Offline agents show as unavailable
- [ ] Print job submission works
- [ ] Error messages display correctly

### Integration Tests
- [ ] Complete flow: Register agent → Select printer → Print
- [ ] Multiple agents work correctly
- [ ] Agent goes offline and comes back online
- [ ] Printer list updates when refreshed

---

## Example Implementation (C# / .NET)

```csharp
// AgentController.cs
[ApiController]
[Route("api/hsprint/agents")]
public class HSPrintAgentController : ControllerBase
{
    private readonly IHSPrintAgentService _agentService;
    private readonly IHttpClientFactory _httpClientFactory;

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAgent([FromBody] RegisterAgentRequest request)
    {
        // Validate IP
        if (!IsValidPrivateIP(request.IpAddress))
            return BadRequest(new { error = "Invalid IP address" });

        // Call HSPrint agent to get info
        var client = _httpClientFactory.CreateClient();
        var url = $"http://{request.IpAddress}:{request.Port}/network/info";
        
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Could not connect to HSPrint agent: {ex.Message}" });
        }

        if (!response.IsSuccessStatusCode)
            return BadRequest(new { error = "HSPrint agent did not respond correctly" });

        var networkInfo = await response.Content.ReadFromJsonAsync<NetworkInfo>();

        // Save to database
        var agent = await _agentService.RegisterOrUpdateAgent(
            userId: GetCurrentUserId(),
            ipAddress: request.IpAddress,
            port: request.Port,
            hostname: networkInfo.Hostname,
            version: networkInfo.Version,
            printers: networkInfo.Printers
        );

        return Ok(new { success = true, agent });
    }

    [HttpPost("print")]
    public async Task<IActionResult> Print([FromBody] PrintRequest request)
    {
        var agent = await _agentService.GetAgent(request.AgentId);
        if (agent == null)
            return NotFound(new { error = "Agent not found" });

        // Verify agent belongs to current user
        if (agent.UserId != GetCurrentUserId())
            return Forbid();

        // Forward print job to HSPrint agent
        var client = _httpClientFactory.CreateClient();
        var endpoint = request.PrintType.ToLower() switch
        {
            "zpl" => "print/zpl",
            "image" => "print/image",
            "pdf" => "print/pdf",
            _ => null
        };

        if (endpoint == null)
            return BadRequest(new { error = "Invalid print type" });

        var url = $"http://{agent.IpAddress}:{agent.Port}/{endpoint}";
        var body = request.PrintType.ToLower() switch
        {
            "zpl" => new { PrinterName = request.PrinterName, Zpl = request.Data },
            "image" => new { PrinterName = request.PrinterName, Base64Png = request.Data },
            "pdf" => new { PrinterName = request.PrinterName, Base64Pdf = request.Data },
            _ => null
        };

        var response = await client.PostAsJsonAsync(url, body);

        if (response.IsSuccessStatusCode)
        {
            return Ok(new { 
                success = true, 
                message = $"Print job sent to {agent.Hostname} - {request.PrinterName}" 
            });
        }

        return BadRequest(new { 
            success = false, 
            error = "Failed to send print job to agent" 
        });
    }
}
```

---

## Next Steps

1. **Implement database schema** (HSPrintAgents and HSPrintAgentPrinters tables)
2. **Create backend API endpoints** (register, list, refresh, delete, print)
3. **Implement background job** for health checks
4. **Create frontend UI** (agent management page, printer dropdown)
5. **Test complete flow** with multiple HSPrint instances
6. **Document for users** how to enable network mode and register agents

---

## Support Documentation for End Users

**Title:** How to Connect HSPrint Agents to Your Account

**Steps:**
1. Install HSPrint on each PC where you have printers
2. Enable network mode:
   - Open `C:\ProgramData\HSPrint\appsettings.json`
   - Set `"EnableNetworkMode": true`
   - Restart HSPrint service
3. Allow through firewall (Windows will prompt)
4. Log into hssoftware.nl
5. Go to Settings → HSPrint Agents
6. Click "Add Agent"
7. Enter the PC's IP address (find with `ipconfig`)
8. Click "Add Agent" - website will discover printers automatically
9. Now you can print to any printer from any location!

---

## Questions?

If you need clarification on any endpoint, data structure, or implementation detail, please ask!
