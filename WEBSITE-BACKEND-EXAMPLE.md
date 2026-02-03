# Website Backend Example - Complete Implementation

## Technology Stack
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- C# 12 / .NET 8

---

## Step 1: Database Models

```csharp
// Models/HSPrintAgent.cs
public class HSPrintAgent
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 5246;
    public string? Version { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime LastSeen { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<HSPrintAgentPrinter> Printers { get; set; } = new List<HSPrintAgentPrinter>();
}

// Models/HSPrintAgentPrinter.cs
public class HSPrintAgentPrinter
{
    public int Id { get; set; }
    public int AgentId { get; set; }
    public string PrinterName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime LastSeen { get; set; }

    // Navigation properties
    public HSPrintAgent Agent { get; set; } = null!;
}
```

---

## Step 2: Database Context

```csharp
// Data/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    public DbSet<HSPrintAgent> HSPrintAgents { get; set; }
    public DbSet<HSPrintAgentPrinter> HSPrintAgentPrinters { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HSPrintAgent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LastSeen);
            entity.HasIndex(e => new { e.UserId, e.IpAddress, e.Port }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Hostname).HasMaxLength(255).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Version).HasMaxLength(50);
        });

        modelBuilder.Entity<HSPrintAgentPrinter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AgentId);
            entity.HasIndex(e => new { e.AgentId, e.PrinterName }).IsUnique();

            entity.HasOne(e => e.Agent)
                .WithMany(a => a.Printers)
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.PrinterName).HasMaxLength(255).IsRequired();
        });
    }
}
```

---

## Step 3: DTOs

```csharp
// DTOs/NetworkInfoDto.cs
public record NetworkInfoDto(
    string Hostname,
    string IpAddress,
    int Port,
    string Version,
    IEnumerable<PrinterInfoDto> Printers,
    DateTime Timestamp
);

public record PrinterInfoDto(
    string Name,
    bool IsDefault
);

// DTOs/RegisterAgentRequest.cs
public record RegisterAgentRequest(
    string IpAddress,
    int Port = 5246
);

// DTOs/AgentDto.cs
public record AgentDto(
    int Id,
    string Hostname,
    string IpAddress,
    int Port,
    string Version,
    bool IsActive,
    DateTime LastSeen,
    IEnumerable<PrinterInfoDto> Printers
);

// DTOs/PrinterDto.cs
public record PrinterDto(
    string Id,
    int AgentId,
    string AgentHostname,
    string AgentIp,
    int AgentPort,
    string PrinterName,
    string DisplayName,
    bool IsDefault,
    bool IsOnline
);

// DTOs/PrintRequest.cs
public record PrintRequest(
    int? AgentId,
    string? PrinterId,
    string PrinterName,
    string PrintType,
    string Data
);
```

---

## Step 4: Service Interface

```csharp
// Services/IHSPrintAgentService.cs
public interface IHSPrintAgentService
{
    Task<AgentDto?> RegisterOrUpdateAgentAsync(int userId, string ipAddress, int port);
    Task<List<AgentDto>> GetUserAgentsAsync(int userId);
    Task<AgentDto?> GetAgentAsync(int agentId, int userId);
    Task<bool> DeleteAgentAsync(int agentId, int userId);
    Task<AgentDto?> RefreshAgentAsync(int agentId, int userId);
    Task<List<PrinterDto>> GetAllUserPrintersAsync(int userId);
    Task<bool> PrintAsync(int userId, PrintRequest request);
    Task UpdateAgentHealthAsync(); // Background job
}
```

---

## Step 5: Service Implementation

```csharp
// Services/HSPrintAgentService.cs
public class HSPrintAgentService : IHSPrintAgentService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HSPrintAgentService> _logger;

    public HSPrintAgentService(
        ApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<HSPrintAgentService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AgentDto?> RegisterOrUpdateAgentAsync(int userId, string ipAddress, int port)
    {
        // Validate IP address
        if (!IsValidPrivateIP(ipAddress))
        {
            _logger.LogWarning("Invalid IP address: {IpAddress}", ipAddress);
            return null;
        }

        // Fetch info from HSPrint agent
        var networkInfo = await GetNetworkInfoFromAgentAsync(ipAddress, port);
        if (networkInfo == null)
        {
            _logger.LogWarning("Could not get network info from {IpAddress}:{Port}", ipAddress, port);
            return null;
        }

        // Find existing or create new
        var agent = await _context.HSPrintAgents
            .Include(a => a.Printers)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IpAddress == ipAddress && a.Port == port);

        if (agent == null)
        {
            agent = new HSPrintAgent
            {
                UserId = userId,
                IpAddress = ipAddress,
                Port = port,
                CreatedAt = DateTime.UtcNow
            };
            _context.HSPrintAgents.Add(agent);
        }

        // Update agent info
        agent.Hostname = networkInfo.Hostname;
        agent.Version = networkInfo.Version;
        agent.IsActive = true;
        agent.LastSeen = DateTime.UtcNow;
        agent.UpdatedAt = DateTime.UtcNow;

        // Update printers
        agent.Printers.Clear();
        foreach (var printer in networkInfo.Printers)
        {
            agent.Printers.Add(new HSPrintAgentPrinter
            {
                PrinterName = printer.Name,
                IsDefault = printer.IsDefault,
                LastSeen = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Registered/updated agent {Hostname} ({IpAddress}:{Port}) for user {UserId}",
            agent.Hostname, agent.IpAddress, agent.Port, userId);

        return MapToDto(agent);
    }

    public async Task<List<AgentDto>> GetUserAgentsAsync(int userId)
    {
        var agents = await _context.HSPrintAgents
            .Include(a => a.Printers)
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Hostname)
            .ToListAsync();

        return agents.Select(MapToDto).ToList();
    }

    public async Task<AgentDto?> GetAgentAsync(int agentId, int userId)
    {
        var agent = await _context.HSPrintAgents
            .Include(a => a.Printers)
            .FirstOrDefaultAsync(a => a.Id == agentId && a.UserId == userId);

        return agent != null ? MapToDto(agent) : null;
    }

    public async Task<bool> DeleteAgentAsync(int agentId, int userId)
    {
        var agent = await _context.HSPrintAgents
            .FirstOrDefaultAsync(a => a.Id == agentId && a.UserId == userId);

        if (agent == null)
            return false;

        _context.HSPrintAgents.Remove(agent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted agent {AgentId} for user {UserId}", agentId, userId);
        return true;
    }

    public async Task<AgentDto?> RefreshAgentAsync(int agentId, int userId)
    {
        var agent = await _context.HSPrintAgents
            .Include(a => a.Printers)
            .FirstOrDefaultAsync(a => a.Id == agentId && a.UserId == userId);

        if (agent == null)
            return null;

        // Fetch latest info
        var networkInfo = await GetNetworkInfoFromAgentAsync(agent.IpAddress, agent.Port);
        if (networkInfo == null)
        {
            agent.IsActive = false;
            await _context.SaveChangesAsync();
            return MapToDto(agent);
        }

        // Update agent
        agent.Hostname = networkInfo.Hostname;
        agent.Version = networkInfo.Version;
        agent.IsActive = true;
        agent.LastSeen = DateTime.UtcNow;
        agent.UpdatedAt = DateTime.UtcNow;

        // Update printers
        agent.Printers.Clear();
        foreach (var printer in networkInfo.Printers)
        {
            agent.Printers.Add(new HSPrintAgentPrinter
            {
                PrinterName = printer.Name,
                IsDefault = printer.IsDefault,
                LastSeen = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return MapToDto(agent);
    }

    public async Task<List<PrinterDto>> GetAllUserPrintersAsync(int userId)
    {
        var agents = await _context.HSPrintAgents
            .Include(a => a.Printers)
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Hostname)
            .ToListAsync();

        var printers = new List<PrinterDto>();
        foreach (var agent in agents)
        {
            foreach (var printer in agent.Printers)
            {
                printers.Add(new PrinterDto(
                    Id: $"{agent.Id}_{printer.PrinterName}",
                    AgentId: agent.Id,
                    AgentHostname: agent.Hostname,
                    AgentIp: agent.IpAddress,
                    AgentPort: agent.Port,
                    PrinterName: printer.PrinterName,
                    DisplayName: $"{agent.Hostname} - {printer.PrinterName}",
                    IsDefault: printer.IsDefault,
                    IsOnline: agent.IsActive
                ));
            }
        }

        return printers;
    }

    public async Task<bool> PrintAsync(int userId, PrintRequest request)
    {
        // Parse printer ID if provided (format: "123_PrinterName")
        int agentId;
        string printerName;

        if (!string.IsNullOrEmpty(request.PrinterId))
        {
            var parts = request.PrinterId.Split('_', 2);
            if (parts.Length != 2 || !int.TryParse(parts[0], out agentId))
                return false;
            printerName = parts[1];
        }
        else if (request.AgentId.HasValue)
        {
            agentId = request.AgentId.Value;
            printerName = request.PrinterName;
        }
        else
        {
            return false;
        }

        // Get agent
        var agent = await _context.HSPrintAgents
            .FirstOrDefaultAsync(a => a.Id == agentId && a.UserId == userId);

        if (agent == null || !agent.IsActive)
            return false;

        // Send print job to HSPrint agent
        return await SendPrintJobAsync(agent.IpAddress, agent.Port, printerName, request.PrintType, request.Data);
    }

    public async Task UpdateAgentHealthAsync()
    {
        var agents = await _context.HSPrintAgents.ToListAsync();

        foreach (var agent in agents)
        {
            var networkInfo = await GetNetworkInfoFromAgentAsync(agent.IpAddress, agent.Port);
            
            if (networkInfo != null)
            {
                agent.IsActive = true;
                agent.LastSeen = DateTime.UtcNow;
            }
            else
            {
                agent.IsActive = false;
            }
        }

        await _context.SaveChangesAsync();
    }

    // Private helper methods

    private async Task<NetworkInfoDto?> GetNetworkInfoFromAgentAsync(string ipAddress, int port)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var url = $"http://{ipAddress}:{port}/network/info";
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<NetworkInfoDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get network info from {IpAddress}:{Port}", ipAddress, port);
        }

        return null;
    }

    private async Task<bool> SendPrintJobAsync(string ipAddress, int port, string printerName, string printType, string data)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var endpoint = printType.ToLower() switch
            {
                "zpl" => "print/zpl",
                "image" => "print/image",
                "pdf" => "print/pdf",
                _ => null
            };

            if (endpoint == null)
                return false;

            object body = printType.ToLower() switch
            {
                "zpl" => new { PrinterName = printerName, Zpl = data },
                "image" => new { PrinterName = printerName, Base64Png = data },
                "pdf" => new { PrinterName = printerName, Base64Pdf = data },
                _ => null
            };

            if (body == null)
                return false;

            var url = $"http://{ipAddress}:{port}/{endpoint}";
            var response = await client.PostAsJsonAsync(url, body);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending print job to {IpAddress}:{Port}", ipAddress, port);
            return false;
        }
    }

    private bool IsValidPrivateIP(string ipAddress)
    {
        if (!System.Net.IPAddress.TryParse(ipAddress, out var ip))
            return false;

        var bytes = ip.GetAddressBytes();
        
        // 10.0.0.0/8
        if (bytes[0] == 10)
            return true;
        
        // 172.16.0.0/12
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            return true;
        
        // 192.168.0.0/16
        if (bytes[0] == 192 && bytes[1] == 168)
            return true;

        return false;
    }

    private AgentDto MapToDto(HSPrintAgent agent)
    {
        return new AgentDto(
            Id: agent.Id,
            Hostname: agent.Hostname,
            IpAddress: agent.IpAddress,
            Port: agent.Port,
            Version: agent.Version ?? "Unknown",
            IsActive: agent.IsActive,
            LastSeen: agent.LastSeen,
            Printers: agent.Printers.Select(p => new PrinterInfoDto(p.PrinterName, p.IsDefault))
        );
    }
}
```

---

## Step 6: Controller

```csharp
// Controllers/HSPrintAgentController.cs
[ApiController]
[Route("api/hsprint/agents")]
[Authorize] // Require authentication
public class HSPrintAgentController : ControllerBase
{
    private readonly IHSPrintAgentService _agentService;
    private readonly ILogger<HSPrintAgentController> _logger;

    public HSPrintAgentController(IHSPrintAgentService agentService, ILogger<HSPrintAgentController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAgent([FromBody] RegisterAgentRequest request)
    {
        var userId = GetCurrentUserId();
        var agent = await _agentService.RegisterOrUpdateAgentAsync(userId, request.IpAddress, request.Port);

        if (agent == null)
            return BadRequest(new { error = "Could not connect to HSPrint agent or invalid IP address" });

        return Ok(new { success = true, agent });
    }

    [HttpGet]
    public async Task<IActionResult> GetAgents()
    {
        var userId = GetCurrentUserId();
        var agents = await _agentService.GetUserAgentsAsync(userId);
        return Ok(new { agents });
    }

    [HttpGet("{agentId}")]
    public async Task<IActionResult> GetAgent(int agentId)
    {
        var userId = GetCurrentUserId();
        var agent = await _agentService.GetAgentAsync(agentId, userId);

        if (agent == null)
            return NotFound(new { error = "Agent not found" });

        return Ok(agent);
    }

    [HttpPost("{agentId}/refresh")]
    public async Task<IActionResult> RefreshAgent(int agentId)
    {
        var userId = GetCurrentUserId();
        var agent = await _agentService.RefreshAgentAsync(agentId, userId);

        if (agent == null)
            return NotFound(new { error = "Agent not found" });

        return Ok(new { success = true, agent });
    }

    [HttpDelete("{agentId}")]
    public async Task<IActionResult> DeleteAgent(int agentId)
    {
        var userId = GetCurrentUserId();
        var success = await _agentService.DeleteAgentAsync(agentId, userId);

        if (!success)
            return NotFound(new { error = "Agent not found" });

        return Ok(new { success = true, message = "Agent deleted successfully" });
    }

    [HttpGet("printers")]
    public async Task<IActionResult> GetAllPrinters()
    {
        var userId = GetCurrentUserId();
        var printers = await _agentService.GetAllUserPrintersAsync(userId);
        return Ok(new { printers });
    }

    [HttpPost("print")]
    public async Task<IActionResult> Print([FromBody] PrintRequest request)
    {
        var userId = GetCurrentUserId();
        var success = await _agentService.PrintAsync(userId, request);

        if (!success)
            return BadRequest(new { success = false, error = "Failed to send print job" });

        return Ok(new { success = true, message = "Print job sent successfully" });
    }

    private int GetCurrentUserId()
    {
        // Get user ID from claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim ?? "0");
    }
}
```

---

## Step 7: Background Service for Health Checks

```csharp
// Services/AgentHealthCheckService.cs
public class AgentHealthCheckService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentHealthCheckService> _logger;

    public AgentHealthCheckService(IServiceProvider serviceProvider, ILogger<AgentHealthCheckService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var agentService = scope.ServiceProvider.GetRequiredService<IHSPrintAgentService>();

                _logger.LogInformation("Running agent health check...");
                await agentService.UpdateAgentHealthAsync();
                _logger.LogInformation("Agent health check completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during agent health check");
            }

            // Run every 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

---

## Step 8: Registration in Program.cs

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
builder.Services.AddScoped<IHSPrintAgentService, HSPrintAgentService>();
builder.Services.AddHostedService<AgentHealthCheckService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## Complete! 🎉

This complete implementation provides:
- ✅ Full database schema with EF Core
- ✅ All CRUD operations for agents
- ✅ Printer discovery and listing
- ✅ Print job routing
- ✅ Background health checks
- ✅ IP validation and security
- ✅ Full error handling and logging
- ✅ Ready to integrate with your existing user authentication

**Next:** Test with Postman or integrate into your existing website!
