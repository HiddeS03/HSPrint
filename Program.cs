using Serilog;
using HSPrint.Services;
using HSPrint.Utils;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
     .AddJsonFile("appsettings.json")
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting HSPrint application");

    // Check for installation and handle updates/cleanup
    await CheckInstallationAsync();

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Read version from version.txt
    string version = "1.0.0";
    try
    {
        if (File.Exists("version.txt"))
        {
            version = (await File.ReadAllTextAsync("version.txt")).Trim();
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not read version.txt, using default version");
    }
    builder.Configuration["Version"] = version;

    // Configure Kestrel to listen only on localhost
    builder.WebHost.ConfigureKestrel(options =>
       {
           options.ListenLocalhost(50246);
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
            var allowedOrigins = new[] { "http://localhost:3001", "https://hssoftware.nl" };
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        });
    });

    // Register application services
    builder.Services.AddScoped<IPrinterService, PrinterService>();
    builder.Services.AddScoped<IPrintService, PrintService>();

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

    Log.Information("HSPrint v{Version} starting on http://localhost:50246", version);

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
                if (File.Exists("version.txt"))
                {
                    currentVersion = (await File.ReadAllTextAsync("version.txt")).Trim();
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
