using System.Text;
using System.Text.Json;
using HSPrint.Models;

namespace HSPrint.Services;

public class NetworkService : INetworkService
{
    private readonly ILogger<NetworkService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public NetworkService(ILogger<NetworkService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> PrintToRemoteNode(string targetIp, int targetPort, string printerName, string printType, string data)
    {
        try
        {
            _logger.LogInformation("Forwarding {PrintType} print job to remote node {IP}:{Port}, printer: {Printer}",
                printType, targetIp, targetPort, printerName);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            var endpoint = printType.ToLower() switch
            {
                "zpl" => "print/zpl",
                "image" => "print/image",
                "pdf" => "print/pdf",
                _ => throw new ArgumentException($"Unsupported print type: {printType}")
            };

            object requestBody = printType.ToLower() switch
            {
                "zpl" => new { PrinterName = printerName, Zpl = data },
                "image" => new { PrinterName = printerName, Base64Png = data },
                "pdf" => new { PrinterName = printerName, Base64Pdf = data },
                _ => throw new ArgumentException($"Unsupported print type: {printType}")
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json"));

            var url = $"http://{targetIp}:{targetPort}/{endpoint}";
            _logger.LogInformation("Sending request to: {Url}", url);

            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully forwarded print job to {IP}:{Port}", targetIp, targetPort);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to forward print job to {IP}:{Port}. Status: {Status}, Response: {Response}",
                    targetIp, targetPort, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forwarding print job to {IP}:{Port}", targetIp, targetPort);
            return false;
        }
    }

    public async Task<NetworkInfo?> GetRemoteNodeInfo(string targetIp, int targetPort)
    {
        try
        {
            _logger.LogInformation("Fetching info from remote node {IP}:{Port}", targetIp, targetPort);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var url = $"http://{targetIp}:{targetPort}/network/info";
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var info = JsonSerializer.Deserialize<NetworkInfo>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Successfully fetched info from {IP}:{Port}", targetIp, targetPort);
                return info;
            }
            else
            {
                _logger.LogWarning("Failed to fetch info from {IP}:{Port}. Status: {Status}",
                    targetIp, targetPort, response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching info from {IP}:{Port}", targetIp, targetPort);
            return null;
        }
    }
}
