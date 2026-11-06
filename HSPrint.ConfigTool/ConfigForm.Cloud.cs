using System.Net.Http.Json;
using System.Text.Json;
using System.Windows.Forms;

namespace HSPrint.ConfigTool;

/// <summary>
/// "Koppelen met Label Genie": printen via internet. The pairing code comes
/// from Label Genie > Printers > Printen vanaf andere apparaten.
/// </summary>
public partial class ConfigForm
{
    private static readonly HttpClient CloudHttp = new() { BaseAddress = new Uri("http://localhost:5246/"), Timeout = TimeSpan.FromSeconds(30) };
    private Button? _btnCloud;
    private Label? _lblCloud;
    private bool _cloudPaired;

    private void SetupCloudPairing()
    {
        _btnCloud = new Button { Location = new Point(190, 48), Size = new Size(200, 27), Text = "Koppelen met Label Genie" };
        _btnCloud.Click += async (s, e) => await OnCloudButton();
        _lblCloud = new Label { Location = new Point(400, 53), Size = new Size(350, 20), Text = "" };
        grpSettings.Controls.Add(_btnCloud);
        grpSettings.Controls.Add(_lblCloud);

        var timer = new System.Windows.Forms.Timer { Interval = 5000 };
        timer.Tick += async (s, e) => await RefreshCloudStatus();
        timer.Start();
        _ = RefreshCloudStatus();
    }

    private async Task RefreshCloudStatus()
    {
        try
        {
            using var doc = JsonDocument.Parse(await CloudHttp.GetStringAsync("cloud/status"));
            var root = doc.RootElement;
            _cloudPaired = root.GetProperty("paired").GetBoolean();
            var error = root.TryGetProperty("lastError", out var e) && e.ValueKind == JsonValueKind.String ? e.GetString() : null;
            _btnCloud!.Text = _cloudPaired ? "Ontkoppelen" : "Koppelen met Label Genie";
            _lblCloud!.Text = !_cloudPaired ? "Printen via internet: niet gekoppeld"
                : error != null ? $"Printen via internet: {error}"
                : "Printen via internet: gekoppeld en actief";
        }
        catch
        {
            _lblCloud!.Text = "Printen via internet: service niet bereikbaar";
        }
    }

    private async Task OnCloudButton()
    {
        try
        {
            if (_cloudPaired)
            {
                if (MessageBox.Show("Deze pc ontkoppelen? Andere apparaten kunnen dan niet meer via internet op deze printers printen.",
                        "HSPrint", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                await CloudHttp.PostAsync("cloud/unpair", null);
            }
            else
            {
                var code = AskCode();
                if (code == null) return;
                var response = await CloudHttp.PostAsJsonAsync("cloud/pair", new { code });
                if (!response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    var message = doc.RootElement.TryGetProperty("error", out var e) ? e.GetString() : "Koppelen mislukt.";
                    MessageBox.Show(message, "HSPrint", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("Gekoppeld! De printers van deze pc staan nu in Label Genie.", "HSPrint", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"De HSPrint-service is niet bereikbaar. Start de service en probeer opnieuw.\n\n{ex.Message}", "HSPrint", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        await RefreshCloudStatus();
    }

    private static string? AskCode()
    {
        using var form = new Form
        {
            Text = "Koppelen met Label Genie",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(360, 150),
        };
        var label = new Label
        {
            Text = "Open Label Genie > Printers > Printen vanaf andere apparaten,\nklik op \"Printer-pc koppelen\" en vul de code in:",
            Location = new Point(15, 15),
            Size = new Size(330, 36),
        };
        var box = new TextBox
        {
            Location = new Point(15, 60),
            Size = new Size(330, 32),
            Font = new Font("Consolas", 16f),
            CharacterCasing = CharacterCasing.Upper,
            MaxLength = 7,
            TextAlign = HorizontalAlignment.Center,
        };
        var ok = new Button { Text = "Koppelen", DialogResult = DialogResult.OK, Location = new Point(165, 110), Size = new Size(90, 28) };
        var cancel = new Button { Text = "Annuleren", DialogResult = DialogResult.Cancel, Location = new Point(260, 110), Size = new Size(85, 28) };
        form.Controls.AddRange(new Control[] { label, box, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        return form.ShowDialog() == DialogResult.OK && box.Text.Trim().Length > 0 ? box.Text.Trim() : null;
    }
}
