using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OpenshiftWebHook.Models;

/// <summary>
/// Prometheus Alertmanager v2 webhook payload. POST /alert/alert ile gönderilir.
/// </summary>
public class AlertPayload
{
    [JsonPropertyName("version")]
    [JsonPropertyOrder(1)]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("groupKey")]
    [JsonPropertyOrder(2)]
    public string GroupKey { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    [JsonPropertyOrder(3)]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("receiver")]
    [JsonPropertyOrder(4)]
    public string Receiver { get; set; } = string.Empty;

    [JsonPropertyName("groupLabels")]
    [JsonPropertyOrder(5)]
    public Dictionary<string, string> GroupLabels { get; set; } = new();

    [JsonPropertyName("commonLabels")]
    [JsonPropertyOrder(6)]
    public Dictionary<string, string> CommonLabels { get; set; } = new();

    [JsonPropertyName("commonAnnotations")]
    [JsonPropertyOrder(7)]
    public Dictionary<string, string> CommonAnnotations { get; set; } = new();

    [JsonPropertyName("externalURL")]
    [JsonPropertyOrder(8)]
    public string ExternalURL { get; set; } = string.Empty;

    [JsonPropertyName("alerts")]
    [JsonPropertyOrder(9)]
    [Required]
    public List<Alert> Alerts { get; set; } = new();
}
