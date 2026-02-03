using HSPrint.Services;
using HSPrint.Utils;
using Serilog;

// Configure Serilog with proper log directory
var baseDir = AppDomain.CurrentDomain.BaseDirectory;
var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "HSPrint", "logs");

// Ensure log directory exists
Directory.CreateDirectory(logDir);

var configuration = new ConfigurationBuilder()
    .SetBasePath(baseDir)
    .AddJsonFile("appsettings.json")
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .WriteTo.File(
        path: Path.Combine(logDir, "hsprinteragent-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting HSPrint application");

    // Check for installation and handle updates/cleanup
    await CheckInstallationAsync();

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add Windows Service support
    builder.Host.UseWindowsService();

    // Read version from version.txt
    string version = "1.0.0";
    try
    {
        var versionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
        if (File.Exists(versionPath))
        {
            version = (await File.ReadAllTextAsync(versionPath)).Trim();
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not read version.txt, using default version");
    }
    builder.Configuration["Version"] = version;

    // Read port from configuration
    var port = builder.Configuration.GetValue<int>("Port", 5246);
    var enableNetworkMode = builder.Configuration.GetValue<bool>("Network:EnableNetworkMode", false);

    // Configure Kestrel to listen on localhost or all interfaces based on network mode
    builder.WebHost.ConfigureKestrel(options =>
       {
           if (enableNetworkMode)
           {
               // Listen on all network interfaces for network mode
               options.ListenAnyIP(port);
               Log.Information("Network mode enabled - Listening on all interfaces at port {Port}", port);
           }
           else
           {
               // Listen only on localhost for local-only mode
               options.ListenLocalhost(port);
               Log.Information("Local-only mode - Listening on localhost at port {Port}", port);
           }
       });

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "HSPrint API",
            Version = version,
            Description = "Local printer agent for HSsoftware.nl - Handles ZPL, Image, and PDF printing",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "HS Software",
                Url = new Uri("https://hssoftware.nl")
            }
        });
    });

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            if (enableNetworkMode)
            {
                // Allow all origins in network mode for inter-PC communication
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            }
            else
            {
                // Restrict to specific origins in local-only mode
                var allowedOrigins = new[] { "http://localhost:3001", "https://hssoftware.nl" };
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
            }
        });
    });

    // Register application services
    builder.Services.AddScoped<IPrinterService, PrinterService>();
    builder.Services.AddScoped<IPrintService, PrintService>();
    builder.Services.AddScoped<INetworkService, NetworkService>();
    
    // Add HttpClientFactory for network communication
    builder.Services.AddHttpClient();

    // Register Updater
    builder.Services.AddSingleton(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<Updater>>();
        var config = sp.GetRequiredService<IConfiguration>();
        var updateUrl = config["UpdateCheckUrl"] ?? "https://hssoftware.nl/api/agent/latest";
        return new Updater(logger, version, updateUrl);
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", $"HSPrint API v{version}");
            c.RoutePrefix = string.Empty; // Serve Swagger UI at root
        });
    }

    app.UseSerilogRequestLogging();

    app.UseCors("AllowFrontend");

    app.MapControllers();

    // Add a simple root endpoint
    app.MapGet("/", () => new
    {
        application = "HSPrint",
        version = version,
        status = "running",
        documentation = "/swagger"
    });

    Log.Information("HSPrint v{Version} starting on http://localhost:{Port}", version, port);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Helper method to check installation state
static async Task CheckInstallationAsync()
{
    try
    {
        // Check if running with --service argument
        var args = Environment.GetCommandLineArgs();
        bool isServiceMode = args.Contains("--service", StringComparer.OrdinalIgnoreCase);

        if (isServiceMode)
        {
            Log.Information("Running in service mode");
        }

        // Check if this is an upgrade scenario
        if (InstallHelper.IsInstalled())
        {
            var installedVersion = InstallHelper.GetInstalledVersion();
            var currentVersion = "1.0.0";

            try
            {
                var versionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
                if (File.Exists(versionPath))
                {
                    currentVersion = (await File.ReadAllTextAsync(versionPath)).Trim();
                }
            }
            catch { }

            Log.Information("Installed version: {InstalledVersion}, Current version: {CurrentVersion}",
              installedVersion, currentVersion);

            // If versions differ, we might be in an upgrade scenario
            if (installedVersion != currentVersion)
            {
                Log.Information("Version mismatch detected - may be an upgrade scenario");
            }
        }
        else
        {
            Log.Information("First-time installation detected");
        }

        // Configure startup if not already configured
        if (!isServiceMode)
        {
            bool startupConfigured = InstallHelper.ConfigureStartup(true);
            if (startupConfigured)
            {
                Log.Information("Startup configuration verified");
            }
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Error checking installation state");
        // Don't fail startup due to installation check errors
    }
}
