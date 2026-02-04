using System.Text.Json.Serialization;

namespace OpenshiftWebHook.Models;

/// <summary>
/// POST /alert/alert yanıt gövdesi.
/// </summary>
public class AlertResponse
{
    [JsonPropertyName("processed")]
    public int Processed { get; set; }

    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
