using Microsoft.AspNetCore.Mvc;
using OpenshiftWebHook.Models;
using OpenshiftWebHook.Services;

namespace OpenshiftWebHook.Controllers;

[ApiController]
[Route("[controller]")]
public class AlertController : ControllerBase
{
    private readonly ISmsService _smsService;
    private readonly ILogger<AlertController> _logger;

    public AlertController(
        ISmsService smsService,
        ILogger<AlertController> logger)
    {
        _smsService = smsService;
        _logger = logger;
    }

    /// <summary>
    /// Alertmanager v2 webhook. Gönderilen payload içindeki firing + critical/warning alarmlar için SMS gönderir.
    /// </summary>
    /// <param name="payload">Alertmanager webhook payload (version, status, alerts[]). alerts[].labels: alertname, namespace, service, severity; annotations: summary.</param>
    /// <returns>İşlenen, atlanan ve toplam alarm sayıları.</returns>
    [HttpPost("alert")]
    [IgnoreAntiforgeryToken] 
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveAlert([FromBody] AlertPayload payload, CancellationToken cancellationToken)
    {
        if (payload == null || payload.Alerts == null || !payload.Alerts.Any())
        {
            _logger.LogWarning("Received empty or invalid alert payload");
            return BadRequest("Invalid alert payload");
        }

        var processedCount = 0;
        var skippedCount = 0;

        foreach (var alert in payload.Alerts)
        {
            if (!ShouldProcessAlert(alert))
            {
                _logger.LogDebug(
                    "Skipping alert {AlertName} - Severity: {Severity}, Status: {Status}",
                    alert.GetLabel("alertname"), alert.GetLabel("severity"), alert.Status);
                skippedCount++;
                continue;
            }

            var smsMessage = GenerateSmsMessage(alert);

            var success = await _smsService.SendSmsAsync(smsMessage, cancellationToken);
            
            if (success)
            {
                processedCount++;
                _logger.LogInformation(
                    "Successfully processed alert {AlertName}",
                    alert.GetLabel("alertname"));
            }
            else
            {
                _logger.LogError(
                    "Failed to send SMS for alert {AlertName}",
                    alert.GetLabel("alertname"));
            }
        }

        return Ok(new AlertResponse
        {
            Processed = processedCount,
            Skipped = skippedCount,
            Total = payload.Alerts.Count
        });
    }

    private bool ShouldProcessAlert(Alert alert)
    {
        // Only process alerts with status=firing
        if (!alert.Status.Equals("firing", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var severity = alert.GetLabel("severity").ToLowerInvariant();
        return severity == "critical" || severity == "warning";
    }

    private string GenerateSmsMessage(Alert alert)
    {
        var alertName = alert.GetLabel("alertname");
        if (string.IsNullOrEmpty(alertName)) alertName = "Unknown";
        
        var namespaceValue = alert.GetLabel("namespace");
        if (string.IsNullOrEmpty(namespaceValue)) namespaceValue = "Unknown";
        
        var service = alert.GetLabel("service");
        if (string.IsNullOrEmpty(service)) service = "Unknown";
        
        var severity = alert.GetLabel("severity");
        if (string.IsNullOrEmpty(severity)) severity = "Unknown";
        
        var summary = alert.GetAnnotation("summary");
        if (string.IsNullOrEmpty(summary)) summary = "No summary available";
        
        var startsAt = alert.StartsAt.ToString("yyyy-MM-dd HH:mm:ss UTC");

        var message = $"[{severity.ToUpper()}] {alertName}\n" +
                     $"NS: {namespaceValue}\n" +
                     $"Svc: {service}\n" +
                     $"Summary: {summary}\n" +
                     $"Started: {startsAt}";

        if (message.Length > 500)
        {
            message = message.Substring(0, 497) + "...";
        }

        return message;
    }
}
