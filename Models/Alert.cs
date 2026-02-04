using System.Text.Json.Serialization;

namespace OpenshiftWebHook.Models;

/// <summary>
/// Tek bir Prometheus alarmı. labels: alertname, namespace, service, severity vb. içerir.
/// </summary>
public class Alert
{
    [JsonPropertyName("status")]
    [JsonPropertyOrder(1)]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("labels")]
    [JsonPropertyOrder(2)]
    public Dictionary<string, string> Labels { get; set; } = new();

    [JsonPropertyName("annotations")]
    [JsonPropertyOrder(3)]
    public Dictionary<string, string> Annotations { get; set; } = new();

    [JsonPropertyName("startsAt")]
    [JsonPropertyOrder(4)]
    public DateTime StartsAt { get; set; }

    [JsonPropertyName("endsAt")]
    [JsonPropertyOrder(5)]
    public DateTime? EndsAt { get; set; }

    [JsonPropertyName("fingerprint")]
    [JsonPropertyOrder(6)]
    public string Fingerprint { get; set; } = string.Empty;

    [JsonPropertyName("generatorURL")]
    [JsonPropertyOrder(7)]
    public string? GeneratorURL { get; set; }

    /// <summary>Labels dictionary'den değer okur (örn. "alertname", "severity").</summary>
    public string GetLabel(string key) => Labels.TryGetValue(key, out var value) ? value : string.Empty;

    /// <summary>Annotations dictionary'den değer okur (örn. "summary").</summary>
    public string GetAnnotation(string key) => Annotations.TryGetValue(key, out var value) ? value : string.Empty;
}
