using System.Text.Json.Serialization;

internal sealed class SyncPreset
{
    [JsonPropertyName("destinations")]
    public List<string> Destinations { get; set; } = [];

    [JsonPropertyName("sourse")]
    public string Sourse { get; set; } = string.Empty;
}
