using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HSPrint.Services;

/// <summary>
/// Printen via internet (zoals de print-server van The Art of Tea).
///
/// Na koppelen met een code uit Label Genie vraagt HSPrint elke 2 seconden aan
/// de database of er labels klaarstaan, geeft daarbij zijn printers door en
/// meldt elke opdracht af. Zo kan elk apparaat (iPad, telefoon, andere pc) op
/// deze printers printen, zonder poorten open te zetten of in hetzelfde netwerk
/// te zitten. HSPrint maakt alleen uitgaande HTTPS-verbindingen.
/// </summary>
public class CloudPrintService : BackgroundService
{
    private static readonly string StateFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "HSPrint", "cloud.json");

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan IdleInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ErrorInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PrinterRefresh = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopes;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<CloudPrintService> _logger;
    private readonly string _supabaseUrl;
    private readonly string _apiKey;
    private readonly string _version;
    private readonly object _lock = new();
    private CloudState _state;
    private DateTime _lastPrinterSync = DateTime.MinValue;

    public CloudPrintService(IServiceScopeFactory scopes, IHttpClientFactory http, IConfiguration config, ILogger<CloudPrintService> logger)
    {
        _scopes = scopes;
        _http = http;
        _logger = logger;
        _supabaseUrl = (config["Cloud:SupabaseUrl"] ?? "").TrimEnd('/');
        _apiKey = config["Cloud:PublishableKey"] ?? "";
        _version = config["Version"] ?? "1.0.0";
        _state = LoadState();
    }

    public CloudStatus Status
    {
        get
        {
            lock (_lock)
            {
                return new CloudStatus(
                    Paired: !string.IsNullOrEmpty(_state.Token),
                    StationId: _state.StationId,
                    LastContact: _state.LastContact,
                    LastError: _state.LastError,
                    JobsPrinted: _state.JobsPrinted);
            }
        }
    }

    /// <summary>Exchange a pairing code from Label Genie for this PC's own key.</summary>
    public async Task<(bool ok, string? error)> PairAsync(string code, CancellationToken ct)
    {
        code = (code ?? "").Trim().ToUpperInvariant().Replace(" ", "").Replace("-", "");
        if (code.Length != 6) return (false, "De code bestaat uit 6 tekens.");

        try
        {
            var result = await Rpc("print_station_pair", new
            {
                p_code = code,
                p_hostname = Environment.MachineName,
                p_version = _version,
            }, ct);

            if (!result.ok) return (false, FriendlyError(result.body));

            var pair = JsonSerializer.Deserialize<PairResult>(result.body);
            if (pair?.Token is null) return (false, "Onverwacht antwoord van de server.");

            lock (_lock)
            {
                _state = new CloudState { Token = pair.Token, StationId = pair.StationId, LastContact = DateTime.UtcNow };
                SaveState(_state);
            }
            _lastPrinterSync = DateTime.MinValue; // send printers right away
            _logger.LogInformation("Gekoppeld met Label Genie (station {StationId})", pair.StationId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Koppelen mislukt");
            return (false, "Geen verbinding met de server. Controleer de internetverbinding.");
        }
    }

    public void Unpair()
    {
        lock (_lock)
        {
            _state = new CloudState();
            SaveState(_state);
        }
        _logger.LogInformation("Ontkoppeld van Label Genie");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrEmpty(_supabaseUrl) || string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogInformation("Printen via internet staat uit (Cloud-instellingen ontbreken)");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan wait;
            try
            {
                wait = await PollOnce(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                lock (_lock) _state.LastError = ex.Message;
                _logger.LogWarning(ex, "Printen via internet: ophalen mislukt");
                wait = ErrorInterval;
            }

            try { await Task.Delay(wait, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task<TimeSpan> PollOnce(CancellationToken ct)
    {
        string? token;
        lock (_lock) token = _state.Token;
        if (string.IsNullOrEmpty(token)) return IdleInterval;

        object? printers = null;
        if (DateTime.UtcNow - _lastPrinterSync > PrinterRefresh)
        {
            using var scope = _scopes.CreateScope();
            var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();
            printers = printerService.ListInstalledPrinters()
                .Select(name => new { name, method = GuessMethod(name) })
                .ToList();
        }

        var result = await Rpc("print_station_poll", new { p_token = token, p_printers = printers, p_version = _version }, ct);
        if (!result.ok)
        {
            if (result.body.Contains("unknown_station"))
            {
                // Removed in Label Genie: forget the key so the user can pair again.
                _logger.LogWarning("Deze pc is ontkoppeld in Label Genie");
                Unpair();
                return IdleInterval;
            }
            lock (_lock) _state.LastError = FriendlyError(result.body);
            return ErrorInterval;
        }

        if (printers != null) _lastPrinterSync = DateTime.UtcNow;

        var poll = JsonSerializer.Deserialize<PollResult>(result.body);
        lock (_lock)
        {
            _state.LastContact = DateTime.UtcNow;
            _state.LastError = null;
        }

        foreach (var job in poll?.Jobs ?? new List<CloudJob>())
        {
            var (ok, error) = await PrintJob(job);
            await Rpc("print_station_complete", new { p_token = token, p_job_id = job.Id, p_ok = ok, p_error = error }, ct);
            if (ok) lock (_lock) _state.JobsPrinted++;
        }

        return PollInterval;
    }

    private async Task<(bool ok, string? error)> PrintJob(CloudJob job)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var print = scope.ServiceProvider.GetRequiredService<IPrintService>();
            _logger.LogInformation("Printopdracht {JobId} ({Method}) naar {Printer}", job.Id, job.Method, job.Printer);

            switch (job.Method)
            {
                case "zpl":
                    var (zplOk, zplError) = await print.PrintZpl(job.Printer, job.Data);
                    return (zplOk, zplError);
                case "image":
                    return (await print.PrintImage(job.Printer, job.Data), "Afbeelding printen mislukt");
                case "pdf":
                    return (await print.PrintPdf(job.Printer, job.Data), "PDF printen mislukt");
                default:
                    return (false, $"Onbekende printmethode '{job.Method}'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Printopdracht {JobId} mislukt", job.Id);
            return (false, ex.Message);
        }
    }

    private async Task<(bool ok, string body)> Rpc(string function, object args, CancellationToken ct)
    {
        var client = _http.CreateClient("cloud-print");
        client.Timeout = TimeSpan.FromSeconds(20);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/rpc/{function}")
        {
            Content = JsonContent.Create(args),
        };
        request.Headers.Add("apikey", _apiKey);
        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        return (response.IsSuccessStatusCode, body);
    }

    /// <summary>Zebra printers understand ZPL; everything else gets an image.</summary>
    private static string GuessMethod(string printerName)
    {
        var n = printerName.ToLowerInvariant();
        return n.Contains("zebra") || n.Contains("zdesigner") ? "zpl" : "image";
    }

    private static string FriendlyError(string body)
    {
        if (body.Contains("invalid_or_expired_code")) return "Deze code klopt niet of is verlopen. Maak een nieuwe code in Label Genie.";
        if (body.Contains("unknown_station")) return "Deze pc is niet meer gekoppeld.";
        return "De server gaf een fout terug.";
    }

    private static CloudState LoadState()
    {
        try
        {
            if (File.Exists(StateFile))
                return JsonSerializer.Deserialize<CloudState>(File.ReadAllText(StateFile)) ?? new CloudState();
        }
        catch
        {
            // Corrupt file: start unpaired.
        }
        return new CloudState();
    }

    private static void SaveState(CloudState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StateFile)!);
        File.WriteAllText(StateFile, JsonSerializer.Serialize(new CloudState { Token = state.Token, StationId = state.StationId }));
    }

    private class CloudState
    {
        public string? Token { get; set; }
        public string? StationId { get; set; }
        [JsonIgnore] public DateTime? LastContact { get; set; }
        [JsonIgnore] public string? LastError { get; set; }
        [JsonIgnore] public int JobsPrinted { get; set; }
    }

    private class PairResult
    {
        [JsonPropertyName("station_id")] public string? StationId { get; set; }
        [JsonPropertyName("token")] public string? Token { get; set; }
    }

    private class PollResult
    {
        [JsonPropertyName("jobs")] public List<CloudJob>? Jobs { get; set; }
    }

    private class CloudJob
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("printer")] public string Printer { get; set; } = "";
        [JsonPropertyName("method")] public string Method { get; set; } = "";
        [JsonPropertyName("data")] public string Data { get; set; } = "";
    }
}

public record CloudStatus(bool Paired, string? StationId, DateTime? LastContact, string? LastError, int JobsPrinted);
